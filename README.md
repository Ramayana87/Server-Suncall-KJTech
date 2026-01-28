# Server-Suncall-KJTech
Socket server kết nối máy chấm công của công ty Suncall

## Tính Năng Mới / New Features

### Tối Ưu Hóa Lọc Dữ Liệu / Data Filtering Optimization
- ✅ Hỗ trợ lọc dữ liệu theo khoảng thời gian
- ✅ Giảm 80-90% thời gian xử lý và truyền tải dữ liệu
- ✅ Form Test Client để test với mockup data (670k+ records)
- ✅ Performance monitoring và logging chi tiết

### Cách Sử Dụng / How to Use

**1. Chạy Server:**
```bash
Server.exe
```

**2. Chạy Test Client (để test với mockup data):**
```bash
Server.exe test
# hoặc / or
LaunchTestClient.bat
```

**3. Hoặc từ menu Server: Tools > Launch Test Client**

## Chi Tiết / Details

Xem hướng dẫn đầy đủ trong [OPTIMIZATION_GUIDE.md](OPTIMIZATION_GUIDE.md)

### Socket Protocol

**Format:**
```
machineNumber|ip|port|fromDate|toDate
```

**Ví dụ:**
```
1|192.168.1.201|4370|2024-09-01 00:00:00|2024-09-30 23:59:59
```

### Hiệu Suất / Performance
- Không lọc: ~670,000 records, ~50MB, ~30-60s
- Có lọc (7 ngày): ~15,000 records, ~2MB, ~3-5s
- **Cải thiện 90%!**

