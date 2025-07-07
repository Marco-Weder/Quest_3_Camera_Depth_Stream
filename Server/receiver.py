import socket
import cv2
import numpy as np
import gzip
import io

TCP_IP, TCP_PORT, UDP_PORT = "127.0.0.1", 8888, 9999

def _pc_ip_towards(mobile_ip: str) -> str:
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    try:
        s.connect((mobile_ip, 1))
        ip = s.getsockname()[0]
    except Exception:
        ip = "127.0.0.1"
    finally:
        s.close()
    return ip

def handshake() -> tuple[str, str]:
    srv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    srv.bind((TCP_IP, TCP_PORT))
    srv.listen(1)
    print(f"Waiting for Unity handshake on {TCP_IP}:{TCP_PORT}...")
    conn, _ = srv.accept()

    mobile_ip = conn.recv(1024).decode("ascii").strip()
    print(f"Got mobile IP: {mobile_ip}")

    pc_ip = _pc_ip_towards(mobile_ip)
    conn.send(pc_ip.encode("ascii"))
    print(f"Sent back PC IP: {pc_ip}")

    conn.close()
    srv.close()
    return mobile_ip, pc_ip

mobile_ip, pc_ip = handshake()

udp_sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
udp_sock.bind((pc_ip, UDP_PORT))
print(f"Listening for UDP packets on {pc_ip}:{UDP_PORT}...")

while True:
    packet, _ = udp_sock.recvfrom(65535)
    parts = packet.split(b";", 8)
    if len(parts) < 9:
        print("Bad header (too few parts)")
        continue

    try:
        timestamp = parts[0].decode("ascii")
        rgb_w, rgb_h, rgb_size = map(int, parts[1:4])
        depth_w, depth_h, depth_size = map(int, parts[4:7])
        compression = parts[7].decode("ascii").strip()
        payload = parts[8]
    except Exception as e:
        print("Header parse failed:", e)
        continue

    if len(payload) < rgb_size + depth_size:
        print(f"Truncated packet: expected at least {rgb_size + depth_size}, got {len(payload)}")
        continue

    rgb_bytes = payload[:rgb_size]
    depth_bytes = payload[rgb_size:rgb_size + depth_size]

    rgb_img = cv2.imdecode(np.frombuffer(rgb_bytes, np.uint8), cv2.IMREAD_COLOR)
    if rgb_img is None:
        print("RGB decode failed")
        continue

    if compression.startswith("gzip"):
        try:
            with gzip.GzipFile(fileobj=io.BytesIO(depth_bytes)) as f:
                depth_raw = f.read()
        except Exception as e:
            print(f"GZip decompress failed: {e}")
            continue
    else:
        depth_raw = depth_bytes

    try:
        depth_arr = np.frombuffer(depth_raw, np.uint16).reshape((depth_h, depth_w)).astype(np.float32)
        depth_arr /= 65535.0
    except Exception as e:
        print(f"Depth reshape failed: {e}")
        continue

    d_min, d_max, d_mean = depth_arr.min(), depth_arr.max(), depth_arr.mean()
    #print(f"Depth stats -> min: {d_min:.4f} max: {d_max:.4f} mean: {d_mean:.4f}")

    if d_max > d_min:
        depth_norm = (depth_arr - d_min) / (d_max - d_min)
    else:
        depth_norm = np.zeros_like(depth_arr)

    depth_vis = (depth_norm * 255).astype(np.uint8)
    depth_col = cv2.applyColorMap(depth_vis, cv2.COLORMAP_JET)

    cv2.putText(rgb_img, timestamp, (10, 30),
                cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)

    depth_resized = cv2.resize(depth_col, (rgb_img.shape[1], rgb_img.shape[0]))
    cv2.imshow("RGB | Depth", np.hstack((rgb_img, depth_resized)))

    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

cv2.destroyAllWindows()
udp_sock.close()
