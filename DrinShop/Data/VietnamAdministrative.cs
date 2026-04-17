using System.Collections.Generic;
using System.Linq;
using DrinkShop.Data;

namespace DrinkShop.Data
{
    public static class VietnamAdministrative
    {
        public static IReadOnlyList<ProvinceInfo> Provinces { get; } = new List<ProvinceInfo>
        {
            // ======== THÀNH PHỐ TRỰC THUỘC TRUNG ƯƠNG ========

            // 1) Hà Nội
            new ProvinceInfo
            {
                Name = "Hà Nội",
                Districts = new List<string>
                {
                    // Quận
                    "Quận Ba Đình","Quận Hoàn Kiếm","Quận Tây Hồ","Quận Long Biên","Quận Cầu Giấy",
                    "Quận Đống Đa","Quận Hai Bà Trưng","Quận Hoàng Mai","Quận Thanh Xuân",
                    "Quận Nam Từ Liêm","Quận Bắc Từ Liêm","Quận Hà Đông",
                    // Thị xã
                    "Thị xã Sơn Tây",
                    // Huyện
                    "Huyện Sóc Sơn","Huyện Đông Anh","Huyện Gia Lâm","Huyện Thanh Trì","Huyện Mê Linh",
                    "Huyện Ba Vì","Huyện Phúc Thọ","Huyện Đan Phượng","Huyện Hoài Đức",
                    "Huyện Quốc Oai","Huyện Thạch Thất","Huyện Chương Mỹ","Huyện Thanh Oai",
                    "Huyện Thường Tín","Huyện Phú Xuyên","Huyện Ứng Hòa","Huyện Mỹ Đức"
                }
            },

            // 2) TP Hồ Chí Minh (trước khi tách/điều chỉnh cấp quận gần đây; đã có Thành phố Thủ Đức)
            new ProvinceInfo
            {
                Name = "TP Hồ Chí Minh",
                Districts = new List<string>
                {
                    "Quận 1","Quận 3","Quận 4","Quận 5","Quận 6","Quận 7","Quận 8","Quận 10","Quận 11","Quận 12",
                    "Quận Bình Thạnh","Quận Gò Vấp","Quận Phú Nhuận","Quận Tân Bình","Quận Tân Phú","Quận Bình Tân",
                    "Thành phố Thủ Đức",
                    "Huyện Bình Chánh","Huyện Cần Giờ","Huyện Củ Chi","Huyện Hóc Môn","Huyện Nhà Bè"
                }
            },

            // 3) Đà Nẵng
            new ProvinceInfo
            {
                Name = "Đà Nẵng",
                Districts = new List<string>
                {
                    "Quận Hải Châu","Quận Thanh Khê","Quận Sơn Trà","Quận Ngũ Hành Sơn",
                    "Quận Liên Chiểu","Quận Cẩm Lệ","Huyện Hòa Vang","Huyện Hoàng Sa"
                }
            },

            // 4) Hải Phòng
            new ProvinceInfo
            {
                Name = "Hải Phòng",
                Districts = new List<string>
                {
                    "Quận Hồng Bàng","Quận Ngô Quyền","Quận Lê Chân","Quận Hải An","Quận Kiến An",
                    "Quận Đồ Sơn","Quận Dương Kinh",
                    "Huyện Thuỷ Nguyên","Huyện An Dương","Huyện An Lão","Huyện Kiến Thuỵ",
                    "Huyện Tiên Lãng","Huyện Vĩnh Bảo","Huyện Cát Hải","Huyện Bạch Long Vĩ"
                }
            },

            // 5) Cần Thơ
            new ProvinceInfo
            {
                Name = "Cần Thơ",
                Districts = new List<string>
                {
                    "Quận Ninh Kiều","Quận Ô Môn","Quận Bình Thuỷ","Quận Cái Răng","Quận Thốt Nốt",
                    "Huyện Vĩnh Thạnh","Huyện Cờ Đỏ","Huyện Phong Điền","Huyện Thới Lai"
                }
            },

            // ======== CÁC TỈNH MIỀN BẮC ========

            new ProvinceInfo { Name = "Hà Giang", Districts = new List<string>
            {
                "Thành phố Hà Giang","Huyện Đồng Văn","Huyện Mèo Vạc","Huyện Yên Minh","Huyện Quản Bạ",
                "Huyện Bắc Mê","Huyện Hoàng Su Phì","Huyện Xín Mần","Huyện Vị Xuyên","Huyện Bắc Quang","Huyện Quang Bình"
            }},

            new ProvinceInfo { Name = "Cao Bằng", Districts = new List<string>
            {
                "Thành phố Cao Bằng","Huyện Bảo Lạc","Huyện Bảo Lâm","Huyện Hà Quảng","Huyện Hạ Lang",
                "Huyện Hoà An","Huyện Nguyên Bình","Huyện Quảng Hoà","Huyện Thạch An"
            }},

            new ProvinceInfo { Name = "Bắc Kạn", Districts = new List<string>
            {
                "Thành phố Bắc Kạn","Huyện Ba Bể","Huyện Bạch Thông","Huyện Chợ Đồn","Huyện Chợ Mới",
                "Huyện Na Rì","Huyện Ngân Sơn","Huyện Pác Nặm"
            }},

            new ProvinceInfo { Name = "Lào Cai", Districts = new List<string>
            {
                "Thành phố Lào Cai","Thị xã Sa Pa","Huyện Bát Xát","Huyện Bảo Thắng","Huyện Bảo Yên",
                "Huyện Văn Bàn","Huyện Bảo Hà (nếu đã thành lập thì cập nhật)","Huyện Mường Khương","Huyện Si Ma Cai"
            }},

            new ProvinceInfo { Name = "Lai Châu", Districts = new List<string>
            {
                "Thành phố Lai Châu","Huyện Mường Tè","Huyện Nậm Nhùn","Huyện Phong Thổ",
                "Huyện Sìn Hồ","Huyện Tam Đường","Huyện Tân Uyên","Huyện Than Uyên"
            }},

            new ProvinceInfo { Name = "Điện Biên", Districts = new List<string>
            {
                "Thành phố Điện Biên Phủ","Thị xã Mường Lay","Huyện Điện Biên","Huyện Điện Biên Đông",
                "Huyện Mường Nhé","Huyện Mường Chà","Huyện Mường Ảng","Huyện Tủa Chùa","Huyện Tuần Giáo","Huyện Nậm Pồ"
            }},

            new ProvinceInfo { Name = "Sơn La", Districts = new List<string>
            {
                "Thành phố Sơn La","Huyện Bắc Yên","Huyện Mai Sơn","Huyện Mộc Châu","Huyện Phù Yên",
                "Huyện Quỳnh Nhai","Huyện Sông Mã","Huyện Sốp Cộp","Huyện Thuận Châu","Huyện Vân Hồ","Huyện Yên Châu"
            }},

            new ProvinceInfo { Name = "Yên Bái", Districts = new List<string>
            {
                "Thành phố Yên Bái","Thị xã Nghĩa Lộ","Huyện Lục Yên","Huyện Mù Cang Chải","Huyện Trạm Tấu",
                "Huyện Văn Chấn","Huyện Văn Yên","Huyện Yên Bình","Huyện Trấn Yên"
            }},

            new ProvinceInfo { Name = "Tuyên Quang", Districts = new List<string>
            {
                "Thành phố Tuyên Quang","Huyện Chiêm Hóa","Huyện Hàm Yên","Huyện Lâm Bình",
                "Huyện Na Hang","Huyện Sơn Dương","Huyện Yên Sơn"
            }},

            new ProvinceInfo { Name = "Lạng Sơn", Districts = new List<string>
            {
                "Thành phố Lạng Sơn","Huyện Bắc Sơn","Huyện Bình Gia","Huyện Cao Lộc","Huyện Chi Lăng",
                "Huyện Đình Lập","Huyện Hữu Lũng","Huyện Lộc Bình","Huyện Tràng Định","Huyện Văn Lãng","Huyện Văn Quan"
            }},

            new ProvinceInfo { Name = "Quảng Ninh", Districts = new List<string>
            {
                "Thành phố Hạ Long","Thành phố Cẩm Phả","Thành phố Uông Bí","Thành phố Móng Cái",
                "Thị xã Đông Triều","Thị xã Quảng Yên",
                "Huyện Bình Liêu","Huyện Ba Chẽ","Huyện Đầm Hà","Huyện Hải Hà","Huyện Tiên Yên","Huyện Vân Đồn","Huyện Cô Tô"
            }},

            new ProvinceInfo { Name = "Bắc Giang", Districts = new List<string>
            {
                "Thành phố Bắc Giang","Huyện Hiệp Hòa","Huyện Lạng Giang","Huyện Lục Nam","Huyện Lục Ngạn",
                "Huyện Sơn Động","Huyện Tân Yên","Huyện Việt Yên","Huyện Yên Dũng","Huyện Yên Thế"
            }},

            new ProvinceInfo { Name = "Phú Thọ", Districts = new List<string>
            {
                "Thành phố Việt Trì","Thị xã Phú Thọ","Huyện Cẩm Khê","Huyện Đoan Hùng","Huyện Hạ Hòa",
                "Huyện Lâm Thao","Huyện Phù Ninh","Huyện Tam Nông","Huyện Tân Sơn","Huyện Thanh Ba",
                "Huyện Thanh Sơn","Huyện Thanh Thủy","Huyện Yên Lập"
            }},

            new ProvinceInfo { Name = "Thái Nguyên", Districts = new List<string>
            {
                "Thành phố Thái Nguyên","Thành phố Sông Công","Thành phố Phổ Yên",
                "Huyện Đại Từ","Huyện Định Hóa","Huyện Đồng Hỷ","Huyện Phú Bình","Huyện Phú Lương","Huyện Võ Nhai"
            }},

            new ProvinceInfo { Name = "Bắc Ninh", Districts = new List<string>
            {
                "Thành phố Bắc Ninh","Thành phố Từ Sơn",
                "Huyện Gia Bình","Huyện Lương Tài","Huyện Quế Võ","Huyện Thuận Thành","Huyện Tiên Du","Huyện Yên Phong"
            }},

            new ProvinceInfo { Name = "Hải Dương", Districts = new List<string>
            {
                "Thành phố Hải Dương","Thành phố Chí Linh",
                "Huyện Bình Giang","Huyện Cẩm Giàng","Huyện Gia Lộc","Huyện Kim Thành","Huyện Kinh Môn (thị xã/thành phố nếu đã nâng cấp)",
                "Huyện Nam Sách","Huyện Ninh Giang","Huyện Thanh Hà","Huyện Thanh Miện","Huyện Tứ Kỳ"
            }},

            new ProvinceInfo { Name = "Hưng Yên", Districts = new List<string>
            {
                "Thành phố Hưng Yên","Thị xã Mỹ Hào",
                "Huyện Ân Thi","Huyện Khoái Châu","Huyện Kim Động","Huyện Phù Cừ","Huyện Tiên Lữ","Huyện Văn Giang","Huyện Văn Lâm","Huyện Yên Mỹ"
            }},

            new ProvinceInfo { Name = "Vĩnh Phúc", Districts = new List<string>
            {
                "Thành phố Vĩnh Yên","Thành phố Phúc Yên",
                "Huyện Bình Xuyên","Huyện Lập Thạch","Huyện Sông Lô","Huyện Tam Dương","Huyện Tam Đảo","Huyện Vĩnh Tường","Huyện Yên Lạc"
            }},

            new ProvinceInfo { Name = "Nam Định", Districts = new List<string>
            {
                "Thành phố Nam Định","Huyện Giao Thủy","Huyện Hải Hậu","Huyện Mỹ Lộc","Huyện Nam Trực",
                "Huyện Nghĩa Hưng","Huyện Trực Ninh","Huyện Vụ Bản","Huyện Xuân Trường","Huyện Ý Yên"
            }},

            new ProvinceInfo { Name = "Ninh Bình", Districts = new List<string>
            {
                "Thành phố Ninh Bình","Thành phố Tam Điệp",
                "Huyện Gia Viễn","Huyện Hoa Lư","Huyện Kim Sơn","Huyện Nho Quan","Huyện Yên Khánh","Huyện Yên Mô"
            }},

            new ProvinceInfo { Name = "Thái Bình", Districts = new List<string>
            {
                "Thành phố Thái Bình","Huyện Đông Hưng","Huyện Hưng Hà","Huyện Kiến Xương",
                "Huyện Quỳnh Phụ","Huyện Thái Thụy","Huyện Tiền Hải","Huyện Vũ Thư"
            }},

            new ProvinceInfo { Name = "Hà Nam", Districts = new List<string>
            {
                "Thành phố Phủ Lý","Thị xã Duy Tiên","Huyện Bình Lục","Huyện Kim Bảng","Huyện Lý Nhân","Huyện Thanh Liêm"
            }},

            new ProvinceInfo { Name = "Hòa Bình", Districts = new List<string>
            {
                "Thành phố Hòa Bình","Huyện Cao Phong","Huyện Đà Bắc","Huyện Kim Bôi","Huyện Kỳ Sơn (nếu còn trước sáp nhập)",
                "Huyện Lạc Sơn","Huyện Lạc Thủy","Huyện Lương Sơn","Huyện Mai Châu","Huyện Tân Lạc","Huyện Yên Thủy"
            }},

            // ======== BẮC TRUNG BỘ & DUYÊN HẢI MIỀN TRUNG ========

            new ProvinceInfo { Name = "Thanh Hóa", Districts = new List<string>
            {
                "Thành phố Thanh Hóa","Thị xã Bỉm Sơn","Thị xã Sầm Sơn",
                "Huyện Bá Thước","Huyện Cẩm Thủy","Huyện Đông Sơn","Huyện Hà Trung","Huyện Hậu Lộc","Huyện Hoằng Hóa",
                "Huyện Lang Chánh","Huyện Mường Lát","Huyện Nga Sơn","Huyện Ngọc Lạc","Huyện Như Thanh","Huyện Như Xuân",
                "Huyện Nông Cống","Huyện Quan Hóa","Huyện Quan Sơn","Huyện Quảng Xương","Huyện Thạch Thành",
                "Huyện Thiệu Hóa","Huyện Thọ Xuân","Huyện Thường Xuân","Huyện Tĩnh Gia (thị xã Nghi Sơn nếu đã nâng cấp)",
                "Huyện Triệu Sơn","Huyện Vĩnh Lộc","Huyện Yên Định"
            }},

            new ProvinceInfo { Name = "Nghệ An", Districts = new List<string>
            {
                "Thành phố Vinh","Thị xã Cửa Lò","Thị xã Thái Hòa","Thị xã Hoàng Mai",
                "Huyện Anh Sơn","Huyện Con Cuông","Huyện Diễn Châu","Huyện Đô Lương","Huyện Hưng Nguyên",
                "Huyện Kỳ Sơn","Huyện Nam Đàn","Huyện Nghi Lộc","Huyện Nghĩa Đàn","Huyện Quế Phong",
                "Huyện Quỳ Châu","Huyện Quỳ Hợp","Huyện Quỳnh Lưu","Huyện Tân Kỳ","Huyện Thanh Chương",
                "Huyện Tương Dương","Huyện Yên Thành"
            }},

            new ProvinceInfo { Name = "Hà Tĩnh", Districts = new List<string>
            {
                "Thành phố Hà Tĩnh","Thị xã Hồng Lĩnh","Thị xã Kỳ Anh",
                "Huyện Cẩm Xuyên","Huyện Can Lộc","Huyện Đức Thọ","Huyện Hương Khê","Huyện Hương Sơn",
                "Huyện Lộc Hà","Huyện Nghi Xuân","Huyện Thạch Hà","Huyện Vũ Quang","Huyện Kỳ Anh"
            }},

            new ProvinceInfo { Name = "Quảng Bình", Districts = new List<string>
            {
                "Thành phố Đồng Hới","Thị xã Ba Đồn",
                "Huyện Bố Trạch","Huyện Lệ Thủy","Huyện Minh Hóa","Huyện Quảng Ninh","Huyện Quảng Trạch","Huyện Tuyên Hóa"
            }},

            new ProvinceInfo { Name = "Quảng Trị", Districts = new List<string>
            {
                "Thành phố Đông Hà","Thị xã Quảng Trị",
                "Huyện Cam Lộ","Huyện Cồn Cỏ","Huyện Đăk Rông","Huyện Gio Linh","Huyện Hải Lăng","Huyện Hướng Hóa","Huyện Triệu Phong","Huyện Vĩnh Linh"
            }},

            new ProvinceInfo { Name = "Thừa Thiên Huế", Districts = new List<string>
            {
                "Thành phố Huế","Thị xã Hương Thủy","Thị xã Hương Trà",
                "Huyện A Lưới","Huyện Nam Đông","Huyện Phong Điền","Huyện Phú Lộc","Huyện Phú Vang","Huyện Quảng Điền"
            }},

            new ProvinceInfo { Name = "Quảng Nam", Districts = new List<string>
            {
                "Thành phố Tam Kỳ","Thành phố Hội An","Thị xã Điện Bàn",
                "Huyện Bắc Trà My","Huyện Duy Xuyên","Huyện Đại Lộc","Huyện Đông Giang","Huyện Hiệp Đức","Huyện Nam Giang",
                "Huyện Nam Trà My","Huyện Nông Sơn","Huyện Núi Thành","Huyện Phú Ninh","Huyện Phước Sơn",
                "Huyện Quế Sơn","Huyện Tây Giang","Huyện Thăng Bình","Huyện Tiên Phước"
            }},

            new ProvinceInfo { Name = "Quảng Ngãi", Districts = new List<string>
            {
                "Thành phố Quảng Ngãi","Thị xã Đức Phổ",
                "Huyện Ba Tơ","Huyện Bình Sơn","Huyện Lý Sơn","Huyện Minh Long","Huyện Mộ Đức",
                "Huyện Nghĩa Hành","Huyện Sơn Hà","Huyện Sơn Tây","Huyện Sơn Tịnh","Huyện Trà Bồng","Huyện Tư Nghĩa"
            }},

            new ProvinceInfo { Name = "Bình Định", Districts = new List<string>
            {
                "Thành phố Quy Nhơn","Thị xã An Nhơn","Thị xã Hoài Nhơn",
                "Huyện An Lão","Huyện Hoài Ân","Huyện Phù Cát","Huyện Phù Mỹ","Huyện Tây Sơn","Huyện Tuy Phước","Huyện Vân Canh","Huyện Vĩnh Thạnh"
            }},

            new ProvinceInfo { Name = "Phú Yên", Districts = new List<string>
            {
                "Thành phố Tuy Hòa","Thị xã Sông Cầu",
                "Huyện Đông Hòa","Huyện Đồng Xuân","Huyện Phú Hòa","Huyện Sơn Hòa","Huyện Sông Hinh","Huyện Tây Hòa","Huyện Tuy An"
            }},

            new ProvinceInfo { Name = "Khánh Hòa", Districts = new List<string>
            {
                "Thành phố Nha Trang","Thành phố Cam Ranh","Thị xã Ninh Hòa",
                "Huyện Cam Lâm","Huyện Diên Khánh","Huyện Khánh Sơn","Huyện Khánh Vĩnh","Huyện Trường Sa","Huyện Vạn Ninh"
            }},

            new ProvinceInfo { Name = "Ninh Thuận", Districts = new List<string>
            {
                "Thành phố Phan Rang - Tháp Chàm","Huyện Bác Ái","Huyện Ninh Hải","Huyện Ninh Phước","Huyện Ninh Sơn","Huyện Thuận Bắc","Huyện Thuận Nam"
            }},

            new ProvinceInfo { Name = "Bình Thuận", Districts = new List<string>
            {
                "Thành phố Phan Thiết","Thị xã La Gi",
                "Huyện Bắc Bình","Huyện Đức Linh","Huyện Hàm Tân","Huyện Hàm Thuận Bắc","Huyện Hàm Thuận Nam","Huyện Phú Quý","Huyện Tánh Linh","Huyện Tuy Phong"
            }},

            // ======== TÂY NGUYÊN ========

            new ProvinceInfo { Name = "Kon Tum", Districts = new List<string>
            {
                "Thành phố Kon Tum","Huyện Đăk Glei","Huyện Đăk Hà","Huyện Đăk Tô","Huyện Ia H’Drai",
                "Huyện Kon Plông","Huyện Kon Rẫy","Huyện Ngọc Hồi","Huyện Sa Thầy","Huyện Tu Mơ Rông"
            }},

            new ProvinceInfo { Name = "Gia Lai", Districts = new List<string>
            {
                "Thành phố Pleiku","Thị xã An Khê","Thị xã Ayun Pa",
                "Huyện Chư Păh","Huyện Chư Prông","Huyện Chư Pưh","Huyện Chư Sê","Huyện Đăk Đoa","Huyện Đăk Pơ",
                "Huyện Đức Cơ","Huyện Ia Grai","Huyện Ia Pa","Huyện KBang","Huyện Kông Chro","Huyện Krông Pa","Huyện Mang Yang","Huyện Phú Thiện"
            }},

            new ProvinceInfo { Name = "Đắk Lắk", Districts = new List<string>
            {
                "Thành phố Buôn Ma Thuột","Thị xã Buôn Hồ",
                "Huyện Buôn Đôn","Huyện Cư Kuin","Huyện Cư M’gar","Huyện Ea H’leo","Huyện Ea Kar","Huyện Ea Súp",
                "Huyện Krông Ana","Huyện Krông Bông","Huyện Krông Buk","Huyện Krông Năng","Huyện Krông Pắc","Huyện Lắk","Huyện M’Đrắk"
            }},

            new ProvinceInfo { Name = "Đắk Nông", Districts = new List<string>
            {
                "Thành phố Gia Nghĩa",
                "Huyện Cư Jút","Huyện Đăk Glong","Huyện Đăk Mil","Huyện Đăk R’lấp","Huyện Đăk Song","Huyện Krông Nô","Huyện Tuy Đức"
            }},

            new ProvinceInfo { Name = "Lâm Đồng", Districts = new List<string>
            {
                "Thành phố Đà Lạt","Thành phố Bảo Lộc",
                "Huyện Bảo Lâm","Huyện Cát Tiên","Huyện Đạ Huoai","Huyện Đạ Tẻh","Huyện Đam Rông",
                "Huyện Di Linh","Huyện Đơn Dương","Huyện Đức Trọng","Huyện Lạc Dương","Huyện Lâm Hà"
            }},

            // ======== ĐÔNG NAM BỘ ========

            new ProvinceInfo { Name = "Bình Phước", Districts = new List<string>
            {
                "Thành phố Đồng Xoài","Thị xã Bình Long","Thị xã Phước Long",
                "Huyện Bù Đăng","Huyện Bù Đốp","Huyện Bù Gia Mập","Huyện Chơn Thành","Huyện Đồng Phú","Huyện Hớn Quản","Huyện Lộc Ninh","Huyện Phú Riềng"
            }},

            new ProvinceInfo { Name = "Tây Ninh", Districts = new List<string>
            {
                "Thành phố Tây Ninh",
                "Huyện Bến Cầu","Huyện Châu Thành","Huyện Dương Minh Châu","Huyện Gò Dầu","Huyện Hòa Thành","Huyện Tân Biên","Huyện Tân Châu","Huyện Trảng Bàng"
            }},

            new ProvinceInfo { Name = "Bình Dương", Districts = new List<string>
            {
                "Thành phố Thủ Dầu Một","Thành phố Dĩ An","Thành phố Thuận An",
                "Thị xã Tân Uyên","Thị xã Bến Cát",
                "Huyện Bàu Bàng","Huyện Bắc Tân Uyên","Huyện Dầu Tiếng","Huyện Phú Giáo"
            }},

            new ProvinceInfo { Name = "Đồng Nai", Districts = new List<string>
            {
                "Thành phố Biên Hòa","Thành phố Long Khánh",
                "Huyện Cẩm Mỹ","Huyện Định Quán","Huyện Long Thành","Huyện Nhơn Trạch","Huyện Tân Phú","Huyện Thống Nhất","Huyện Trảng Bom","Huyện Vĩnh Cửu","Huyện Xuân Lộc"
            }},

            new ProvinceInfo { Name = "Bà Rịa - Vũng Tàu", Districts = new List<string>
            {
                "Thành phố Vũng Tàu","Thành phố Bà Rịa",
                "Thị xã Phú Mỹ",
                "Huyện Châu Đức","Huyện Côn Đảo","Huyện Đất Đỏ","Huyện Long Điền","Huyện Xuyên Mộc"
            }},

            // ======== ĐỒNG BẰNG SÔNG CỬU LONG ========

            new ProvinceInfo { Name = "Long An", Districts = new List<string>
            {
                "Thành phố Tân An",
                "Thị xã Kiến Tường",
                "Huyện Bến Lức","Huyện Cần Đước","Huyện Cần Giuộc","Huyện Châu Thành","Huyện Đức Hòa","Huyện Đức Huệ",
                "Huyện Mộc Hóa","Huyện Tân Hưng","Huyện Tân Thạnh","Huyện Tân Trụ","Huyện Thạnh Hóa","Huyện Thủ Thừa","Huyện Vĩnh Hưng"
            }},

            new ProvinceInfo { Name = "Tiền Giang", Districts = new List<string>
            {
                "Thành phố Mỹ Tho","Thị xã Gò Công","Thị xã Cai Lậy",
                "Huyện Cái Bè","Huyện Cai Lậy","Huyện Châu Thành","Huyện Chợ Gạo","Huyện Gò Công Đông","Huyện Gò Công Tây","Huyện Tân Phú Đông","Huyện Tân Phước"
            }},

            new ProvinceInfo { Name = "Bến Tre", Districts = new List<string>
            {
                "Thành phố Bến Tre",
                "Huyện Ba Tri","Huyện Bình Đại","Huyện Châu Thành","Huyện Chợ Lách","Huyện Giồng Trôm",
                "Huyện Mỏ Cày Bắc","Huyện Mỏ Cày Nam","Huyện Thạnh Phú"
            }},

            new ProvinceInfo { Name = "Trà Vinh", Districts = new List<string>
            {
                "Thành phố Trà Vinh","Thị xã Duyên Hải",
                "Huyện Càng Long","Huyện Cầu Kè","Huyện Cầu Ngang","Huyện Châu Thành","Huyện Duyên Hải","Huyện Tiểu Cần","Huyện Trà Cú"
            }},

            new ProvinceInfo { Name = "Vĩnh Long", Districts = new List<string>
            {
                "Thành phố Vĩnh Long","Thị xã Bình Minh",
                "Huyện Bình Tân","Huyện Long Hồ","Huyện Mang Thít","Huyện Tam Bình","Huyện Trà Ôn","Huyện Vũng Liêm"
            }},

            new ProvinceInfo { Name = "Đồng Tháp", Districts = new List<string>
            {
                "Thành phố Cao Lãnh","Thành phố Sa Đéc","Thành phố Hồng Ngự",
                "Huyện Cao Lãnh","Huyện Châu Thành","Huyện Hồng Ngự","Huyện Lai Vung","Huyện Lấp Vò",
                "Huyện Tân Hồng","Huyện Tam Nông","Huyện Thanh Bình","Huyện Tháp Mười"
            }},

            new ProvinceInfo { Name = "An Giang", Districts = new List<string>
            {
                "Thành phố Long Xuyên","Thành phố Châu Đốc",
                "Thị xã Tân Châu",
                "Huyện An Phú","Huyện Châu Phú","Huyện Châu Thành","Huyện Chợ Mới","Huyện Phú Tân","Huyện Thoại Sơn","Huyện Tịnh Biên","Huyện Tri Tôn"
            }},

            new ProvinceInfo { Name = "Kiên Giang", Districts = new List<string>
            {
                "Thành phố Rạch Giá","Thành phố Phú Quốc",
                "Thị xã Hà Tiên",
                "Huyện An Biên","Huyện An Minh","Huyện Châu Thành","Huyện Giang Thành","Huyện Giồng Riềng",
                "Huyện Gò Quao","Huyện Hòn Đất","Huyện Kiên Hải","Huyện Kiên Lương","Huyện Tân Hiệp","Huyện U Minh Thượng","Huyện Vĩnh Thuận"
            }},

            new ProvinceInfo { Name = "Hậu Giang", Districts = new List<string>
            {
                "Thành phố Vị Thanh","Thành phố Ngã Bảy",
                "Huyện Châu Thành","Huyện Châu Thành A","Huyện Long Mỹ","Huyện Phụng Hiệp","Huyện Vị Thủy","Thị xã Long Mỹ"
            }},

            new ProvinceInfo { Name = "Sóc Trăng", Districts = new List<string>
            {
                "Thành phố Sóc Trăng","Thị xã Vĩnh Châu","Thị xã Ngã Năm",
                "Huyện Châu Thành","Huyện Cù Lao Dung","Huyện Kế Sách","Huyện Long Phú","Huyện Mỹ Tú","Huyện Mỹ Xuyên","Huyện Thạnh Trị","Huyện Trần Đề"
            }},

            new ProvinceInfo { Name = "Bạc Liêu", Districts = new List<string>
            {
                "Thành phố Bạc Liêu","Thị xã Giá Rai",
                "Huyện Đông Hải","Huyện Hòa Bình","Huyện Hồng Dân","Huyện Phước Long","Huyện Vĩnh Lợi"
            }},

            new ProvinceInfo { Name = "Cà Mau", Districts = new List<string>
            {
                "Thành phố Cà Mau",
                "Huyện Cái Nước","Huyện Đầm Dơi","Huyện Năm Căn","Huyện Ngọc Hiển","Huyện Phú Tân","Huyện Thới Bình","Huyện Trần Văn Thời","Huyện U Minh"
            }} 
        };

        public static IReadOnlyList<string> GetProvinces() => (IReadOnlyList<string>)System.Linq.Enumerable.ToList(System.Linq.Enumerable.Select(Provinces, p => p.Name));
        public static IReadOnlyList<string> GetDistricts(string province)
        {
            var p = System.Linq.Enumerable.FirstOrDefault(Provinces, x => x.Name == province);
            return p?.Districts ?? new List<string>();
        }
    }

    public class ProvinceInfo
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Districts { get; set; } = new();
    }
}
