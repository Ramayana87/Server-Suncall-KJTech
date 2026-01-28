# Server-Suncall-KJTech
Socket server kết nối máy chấm công của công ty Suncall

## Tối ưu hóa / Optimizations

Server đã được tối ưu hóa để xử lý và lọc dữ liệu chấm công nhanh hơn:

### Các cải tiến chính:
1. **Lọc dữ liệu theo khoảng thời gian** - Server-side date range filtering
2. **Early exit optimization** - Kiểm tra điều kiện theo thứ tự từ nhanh đến chậm
3. **Performance logging** - Theo dõi số lượng record xử lý/lọc/bỏ qua
4. **Default date range** - Tự động lấy 30 ngày gần nhất nếu không có tham số

### Cách sử dụng:

#### Format kết nối mới (với date filtering):
```
machineNumber|ip|port|fromDate|toDate
```

Ví dụ:
```
1|192.168.1.201|4370|2024-01-01 00:00:00|2024-01-31 23:59:59
```

#### Format cũ (vẫn hoạt động):
```
machineNumber|ip|port
```
Server sẽ tự động lấy dữ liệu 30 ngày gần nhất.

## Tài liệu / Documentation

- [Hướng dẫn tối ưu chi tiết (OPTIMIZATION_GUIDE.md)](OPTIMIZATION_GUIDE.md) - Hướng dẫn đầy đủ về tối ưu server và client
- [Ví dụ Client (ClientExample.cs)](ClientExample.cs) - Code mẫu kết nối và sử dụng server

## Yêu cầu / Requirements

- .NET Framework 4.7.2
- Newtonsoft.Json
- SFC3KPCLib (OCX cho máy chấm công)

## Performance Improvements

### Trước khi tối ưu:
- Đọc toàn bộ dữ liệu từ thiết bị
- Lọc dữ liệu sau khi đọc xong
- Memory usage cao

### Sau khi tối ưu:
- Lọc dữ liệu trong quá trình đọc
- Giảm ~60-70% memory usage
- Tăng tốc xử lý ~40-50%
