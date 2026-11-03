using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;

namespace CraftTrip.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(CraftTripDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        // 1. Seed Roles
        var roles = new[] { "Admin", "Host", "Guest" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole(role));
            }
        }

        // 2. Seed Users
        ApplicationUser? adminUser = await userManager.FindByEmailAsync("admin@crafttrip.vn");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@crafttrip.vn",
                Email = "admin@crafttrip.vn",
                FullName = "Quản Trị Viên CraftTrip",
                UserRole = UserRole.Admin,
                EmailConfirmed = true,
                AvatarUrl = "https://images.unsplash.com/photo-1534528741775-53994a69daeb?auto=format&fit=crop&w=400&q=80"
            };
            var result = await userManager.CreateAsync(adminUser, "CraftTrip@2026!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        ApplicationUser? hostUser1 = await userManager.FindByEmailAsync("host.battrang@crafttrip.vn");
        if (hostUser1 == null)
        {
            hostUser1 = new ApplicationUser
            {
                UserName = "host.battrang@crafttrip.vn",
                Email = "host.battrang@crafttrip.vn",
                FullName = "Nguyễn Văn An",
                PhoneNumber = "0912345678",
                UserRole = UserRole.Host,
                EmailConfirmed = true,
                AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=400&q=80"
            };
            var result = await userManager.CreateAsync(hostUser1, "CraftTrip@2026!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(hostUser1, "Host");
            }
        }

        ApplicationUser? hostUser2 = await userManager.FindByEmailAsync("host.vanphuc@crafttrip.vn");
        if (hostUser2 == null)
        {
            hostUser2 = new ApplicationUser
            {
                UserName = "host.vanphuc@crafttrip.vn",
                Email = "host.vanphuc@crafttrip.vn",
                FullName = "Trần Thị Mai",
                PhoneNumber = "0987654321",
                UserRole = UserRole.Host,
                EmailConfirmed = true,
                AvatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=400&q=80"
            };
            var result = await userManager.CreateAsync(hostUser2, "CraftTrip@2026!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(hostUser2, "Host");
            }
        }

        ApplicationUser? guestUser = await userManager.FindByEmailAsync("guest@crafttrip.vn");
        if (guestUser == null)
        {
            guestUser = new ApplicationUser
            {
                UserName = "guest@crafttrip.vn",
                Email = "guest@crafttrip.vn",
                FullName = "Lê Thanh Trúc",
                PhoneNumber = "0905112233",
                UserRole = UserRole.Guest,
                EmailConfirmed = true,
                AvatarUrl = "https://images.unsplash.com/photo-1494790108377-be9c29b29330?auto=format&fit=crop&w=400&q=80"
            };
            var result = await userManager.CreateAsync(guestUser, "CraftTrip@2026!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(guestUser, "Guest");
            }
        }

        // 3. Seed Villages
        if (!await context.Villages.AnyAsync())
        {
            var vBatTrang = new Village
            {
                Name = "Làng gốm Bát Tràng",
                Slug = "lang-gom-bat-trang",
                Location = "Gia Lâm, Hà Nội",
                Address = "Xã Bát Tràng, Huyện Gia Lâm, TP. Hà Nội",
                ShortDescription = "Làng nghề gốm sứ hơn 700 năm tuổi ven dòng sông Hồng, nổi tiếng với kỹ thuật chuốt gốm và dòng men rạn lam cổ truyền.",
                HistoryDescription = "Được hình thành từ thời nhà Lý (thế kỷ XI - XII), Bát Tràng là cái nôi sản sinh ra những tuyệt tác gốm sứ hoàng gia và dân gian của Đại Việt. Trải qua bao thế kỷ thăng trầm, làng nghề vẫn giữ trọn ngọn lửa lò bầu và kỹ nghệ tinh xảo.",
                CraftProcessDescription = "Quy trình làm gốm gồm 5 công đoạn nghiêm ngặt: Chọn và xử lý đất sét trắng -> Tạo hình trên bàn xoay thủ công hoặc đúc khuôn -> Tiện gọt và phơi khô -> Vẽ họa tiết bằng men lam, men rạn -> Nung trong lò ở nhiệt độ 1.200 - 1.300°C.",
                CoverImageUrl = "https://images.unsplash.com/photo-1578749556568-bc2c40e68b61?auto=format&fit=crop&w=1200&q=80",
                IsFeatured = true,
                DisplayOrder = 1
            };

            var vVanPhuc = new Village
            {
                Name = "Làng lụa Vạn Phúc",
                Slug = "lang-lua-van-phuc",
                Location = "Hà Đông, Hà Nội",
                Address = "Phường Vạn Phúc, Quận Hà Đông, TP. Hà Nội",
                ShortDescription = "Làng dệt lụa tơ tằm cổ kính hơn 1.000 năm tuổi, biểu tượng thanh lịch của đất Hà Thành với lụa vân nức tiếng tiến vua.",
                HistoryDescription = "Do bà tổ nghề Lã Thị Nga khởi dựng từ thời nhà Đinh. Lụa Vạn Phúc từng tham dự hội chợ quốc tế Marseille (Pháp) năm 1931 và được người Pháp đánh giá là loại tơ lụa tinh xảo bậc nhất xứ Đông Dương.",
                CraftProcessDescription = "Quy trình dệt lụa gồm: Ươm tơ tằm nguyên chất -> Mắc cửi và dệt trên khung cửi cổ truyền -> Nấu tẩy keo tơ -> Nhuộm màu thảo mộc thiên nhiên -> Phơi lụa thủ công dưới nắng sớm.",
                CoverImageUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?auto=format&fit=crop&w=1200&q=80",
                IsFeatured = true,
                DisplayOrder = 2
            };

            var vPhuVinh = new Village
            {
                Name = "Làng mây tre đan Phú Vinh",
                Slug = "lang-may-tre-dan-phu-vinh",
                Location = "Chương Mỹ, Hà Nội",
                Address = "Xã Phú Nghĩa, Huyện Chương Mỹ, TP. Hà Nội",
                ShortDescription = "Vương quốc mây tre đan với những tác phẩm đan nong, đan hoa tinh xảo như bức tranh thêu của các nghệ nhân tài hoa.",
                HistoryDescription = "Khởi phát từ thế kỷ XVII, Phú Vinh được mệnh danh là xứ sở của những sợi mây mềm dẻo. Nghệ nhân nơi đây có thể biến cây tre, giang, mây thành bất kỳ hình dáng tinh xảo nào.",
                CraftProcessDescription = "Chọn tre nứa dẻo dai -> Cạo tinh và chẻ nan mảnh như sợi chỉ -> Chuốt nan bóng mịn -> Đan tạo hình thủ công -> Hun khói rơm tạo màu tự nhiên và chống mối mọt.",
                CoverImageUrl = "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?auto=format&fit=crop&w=1200&q=80",
                IsFeatured = true,
                DisplayOrder = 3
            };

            var vHaThai = new Village
            {
                Name = "Làng sơn mài Hạ Thái",
                Slug = "lang-son-mai-ha-thai",
                Location = "Thường Tín, Hà Nội",
                Address = "Xã Duyên Thái, Huyện Thường Tín, TP. Hà Nội",
                ShortDescription = "Nghệ thuật sơn ta, cẩn ốc và dát vàng bạc truyền thống mang chiều sâu óng ánh huyền ảo của văn hóa Á Đông.",
                HistoryDescription = "Xuất hiện từ khoảng thế kỷ XVII, làng nghề ban đầu chuyên sơn son thếp vàng đồ thờ cúng, tượng Phật. Sang thế kỷ XX, các nghệ nhân Hạ Thái đã sáng tạo thêm kỹ thuật mài nước độc đáo.",
                CraftProcessDescription = "Làm mộc cốt gỗ -> Đắp vải màn và hom đất phù sa -> Phủ từng lớp sơn ta truyền thống -> Dát vàng, cẩn xà cừ hoặc vẽ trứng -> Mài dưới nước hàng chục lần -> Đánh bóng bằng lòng bàn tay.",
                CoverImageUrl = "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?auto=format&fit=crop&w=1200&q=80",
                IsFeatured = true,
                DisplayOrder = 4
            };

            var vDongHo = new Village
            {
                Name = "Làng tranh dân gian Đông Hồ",
                Slug = "lang-tranh-dan-gian-dong-ho",
                Location = "Thuận Thành, Bắc Ninh",
                Address = "Xã Song Hồ, Thị xã Thuận Thành, Tỉnh Bắc Ninh",
                ShortDescription = "Nét khắc gỗ dân gian trên giấy điệp lấp lánh vỏ sò, biểu tượng của sự may mắn, ấm no trong nếp nhà Việt.",
                HistoryDescription = "Làng tranh nằm nép mình bên dòng sông Đuống, thịnh vượng từ thế kỷ XVI. Mỗi dịp Tết đến xuân về, chợ tranh Đông Hồ nhộn nhịp kẻ bán người mua câu chúc phúc lộc bình an.",
                CraftProcessDescription = "Làm giấy điệp từ vỏ sò quét nước hồ điệp -> Khắc bản in gỗ thị -> Chế màu từ tự nhiên (than lá tre, hoa hòe, sỏi son) -> In từng màu theo từng bản khắc gỗ -> Phơi khô tự nhiên.",
                CoverImageUrl = "https://images.unsplash.com/photo-1582560475093-ba66accbc424?auto=format&fit=crop&w=1200&q=80",
                IsFeatured = true,
                DisplayOrder = 5
            };

            var vChuong = new Village
            {
                Name = "Làng nón Chuông",
                Slug = "lang-non-chuong",
                Location = "Thanh Oai, Hà Nội",
                Address = "Xã Phương Trung, Huyện Thanh Oai, TP. Hà Nội",
                ShortDescription = "Làng nghề làm nón lá truyền thống nổi tiếng với nón bài thơ, nón quai thao duyên dáng của người phụ nữ Việt Nam.",
                HistoryDescription = "Làng Chuông làm nón từ thế kỷ thứ VIII. Những chiếc nón làng Chuông từng được dâng cho các bậc cung tần, hoàng hậu và là bạn đồng hành cùng tà áo dài thướt tha qua bao thế hệ.",
                CraftProcessDescription = "Vò lá nón trong cát mịn -> Phơi sương và ủi lá phẳng bằng giẻ nóng -> Uốn 16 vành tre tròn đều -> Xếp lá và chằm nón bằng chỉ cước mảnh -> Quét dầu thông bóng chống thấm nước.",
                CoverImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=1200&q=80",
                IsFeatured = true,
                DisplayOrder = 6
            };

            context.Villages.AddRange(vBatTrang, vVanPhuc, vPhuVinh, vHaThai, vDongHo, vChuong);
            await context.SaveChangesAsync();
        }

        // 4. Seed HostProfiles
        var batTrangVillage = await context.Villages.FirstOrDefaultAsync(v => v.Slug == "lang-gom-bat-trang");
        var vanPhucVillage = await context.Villages.FirstOrDefaultAsync(v => v.Slug == "lang-lua-van-phuc");

        if (hostUser1 != null && !await context.HostProfiles.AnyAsync(h => h.UserId == hostUser1.Id))
        {
            context.HostProfiles.Add(new HostProfile
            {
                UserId = hostUser1.Id,
                StudioName = "Xưởng Gốm Nghệ Nhân An Bát Tràng",
                CraftType = "Gốm sứ thủ công & men rạn lam cổ",
                Bio = "Nghệ nhân Nguyễn Văn An có hơn 35 năm gắn bó cùng bàn xoay gốm Bát Tràng. Xưởng luôn mở rộng cửa đón khách phương xa đến chạm tay vào đất sét và cảm nhận hơi ấm lò nung.",
                VillageId = batTrangVillage?.Id,
                Address = "Số 18, Ngõ Cổ, Thôn 1 Làng Cổ Bát Tràng, Gia Lâm, Hà Nội",
                VerificationStatus = HostVerificationStatus.Verified,
                BankAccountName = "NGUYEN VAN AN",
                BankAccountNumber = "19028889998888",
                BankName = "Techcombank",
                WalletBalance = 2500000m,
                TotalEarned = 15800000m
            });
        }

        if (hostUser2 != null && !await context.HostProfiles.AnyAsync(h => h.UserId == hostUser2.Id))
        {
            context.HostProfiles.Add(new HostProfile
            {
                UserId = hostUser2.Id,
                StudioName = "Cơ Sở Dệt Lụa Tơ Tằm Cổ Truyền Mai Loan",
                CraftType = "Dệt lụa tơ tằm & Lụa vân Vạn Phúc",
                Bio = "Gìn giữ khung cửi gỗ và kỹ nghệ dệt lụa hoa chìm truyền thống. Cơ sở Mai Loan tự hào mang tới những trải nghiệm chân thực nhất về vòng đời kén tằm và sợi chỉ vàng óng ả.",
                VillageId = vanPhucVillage?.Id,
                Address = "Phố Lụa Vạn Phúc, Phường Vạn Phúc, Hà Đông, Hà Nội",
                VerificationStatus = HostVerificationStatus.Verified,
                BankAccountName = "TRAN THI MAI",
                BankAccountNumber = "101999888777",
                BankName = "Vietcombank",
                WalletBalance = 3200000m,
                TotalEarned = 18600000m
            });
        }
        await context.SaveChangesAsync();

        // 5. Seed Craft Categories
        if (!await context.CraftCategories.AnyAsync())
        {
            var categories = new[]
            {
                new CraftCategory
                {
                    Name = "Gốm sứ cổ truyền",
                    Slug = "gom-su-co-truyen",
                    IconClass = "bi-flower1",
                    ImageUrl = "https://images.unsplash.com/photo-1578749556568-bc2c40e68b61?auto=format&fit=crop&w=600&q=80",
                    Description = "Vuốt gốm trên bàn xoay, tạo hình đất sét và tráng men lam, men rạn truyền thống.",
                    DisplayOrder = 1
                },
                new CraftCategory
                {
                    Name = "Dệt lụa tơ tằm",
                    Slug = "det-lua-to-tam",
                    IconClass = "bi-palette",
                    ImageUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?auto=format&fit=crop&w=600&q=80",
                    Description = "Tìm hiểu vòng đời kén tằm, ươm tơ, dệt lụa và nhuộm màu thảo mộc thiên nhiên.",
                    DisplayOrder = 2
                },
                new CraftCategory
                {
                    Name = "Mây tre đan nghệ thuật",
                    Slug = "may-tre-dan-nghe-thuat",
                    IconClass = "bi-basket",
                    ImageUrl = "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?auto=format&fit=crop&w=600&q=80",
                    Description = "Tự tay chuốt nan giang, đan giỏ tre, khay trà hoa sen mang về làm kỷ niệm.",
                    DisplayOrder = 3
                },
                new CraftCategory
                {
                    Name = "Sơn mài truyền thống",
                    Slug = "son-mai-truyen-thong",
                    IconClass = "bi-brush",
                    ImageUrl = "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?auto=format&fit=crop&w=600&q=80",
                    Description = "Khám phá kỹ thuật sơn ta bí truyền, dát vàng bạc, cẩn trứng và mài tranh dưới nước.",
                    DisplayOrder = 4
                },
                new CraftCategory
                {
                    Name = "Tranh dân gian Đông Hồ",
                    Slug = "tranh-dan-gian-dong-ho",
                    IconClass = "bi-card-image",
                    ImageUrl = "https://images.unsplash.com/photo-1582560475093-ba66accbc424?auto=format&fit=crop&w=600&q=80",
                    Description = "Khắc ván in gỗ, pha màu vỏ sò điệp và in tranh Tết rước tài lộc may mắn.",
                    DisplayOrder = 5
                },
                new CraftCategory
                {
                    Name = "Làm nón lá truyền thống",
                    Slug = "lam-non-la-truyen-thong",
                    IconClass = "bi-sun",
                    ImageUrl = "https://images.unsplash.com/photo-1528127269322-539801943592?auto=format&fit=crop&w=600&q=80",
                    Description = "Ủi lá cọ, lồng vành tre và chằm từng đường kim mũi chỉ tạo nên chiếc nón bài thơ.",
                    DisplayOrder = 6
                }
            };
            context.CraftCategories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        // 6. Seed Artisan Profiles
        if (!await context.ArtisanProfiles.AnyAsync())
        {
            context.ArtisanProfiles.AddRange(
                new ArtisanProfile
                {
                    FullName = "Nguyễn Văn An",
                    Title = "Nghệ nhân Ưu tú Gốm Bát Tràng",
                    AvatarUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=400&q=80",
                    YearsOfExperience = 35,
                    Story = "Sinh ra và lớn lên trong tiếng quay của bàn xoay gốm, nghệ nhân An dành cả cuộc đời để nghiên cứu các bài men lam thời Lê và truyền cảm hứng làm gốm cho hàng ngàn bạn trẻ mỗi năm.",
                    VillageId = batTrangVillage?.Id,
                    HostUserId = hostUser1?.Id,
                    DisplayOrder = 1
                },
                new ArtisanProfile
                {
                    FullName = "Trần Thị Mai",
                    Title = "Nghệ nhân Dệt lụa Vạn Phúc",
                    AvatarUrl = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=400&q=80",
                    YearsOfExperience = 28,
                    Story = "Người con gái Hà Đông đã có gần ba thập kỷ bên khung dệt gỗ cổ kính, phục hồi thành công hoa văn mây lượn trên tơ tằm nguyên chất.",
                    VillageId = vanPhucVillage?.Id,
                    HostUserId = hostUser2?.Id,
                    DisplayOrder = 2
                }
            );
            await context.SaveChangesAsync();
        }

        // 7. Seed Workshops
        var catGom = await context.CraftCategories.FirstOrDefaultAsync(c => c.Slug == "gom-su-co-truyen");
        var catLua = await context.CraftCategories.FirstOrDefaultAsync(c => c.Slug == "det-lua-to-tam");
        var catMay = await context.CraftCategories.FirstOrDefaultAsync(c => c.Slug == "may-tre-dan-nghe-thuat");
        var catSon = await context.CraftCategories.FirstOrDefaultAsync(c => c.Slug == "son-mai-truyen-thong");
        var catTranh = await context.CraftCategories.FirstOrDefaultAsync(c => c.Slug == "tranh-dan-gian-dong-ho");
        var catNon = await context.CraftCategories.FirstOrDefaultAsync(c => c.Slug == "lam-non-la-truyen-thong");

        if (!await context.Workshops.AnyAsync())
        {
            var ws1 = new Workshop
            {
                HostId = hostUser1!.Id,
                CategoryId = catGom!.Id,
                VillageId = batTrangVillage?.Id,
                Title = "Tự tay vuốt gốm Bát Tràng & vẽ men lam cổ cùng nghệ nhân",
                Slug = "tu-tay-vuot-gom-bat-trang-va-ve-men-lam-co",
                ShortDescription = "Trải nghiệm 2,5 giờ ngồi bên bàn xoay truyền thống, chuốt ấm chén gốm và tự tay pha vẽ hoa văn men lam mang về.",
                FullDescription = "Một buổi trải nghiệm đắm mình trong không gian xưởng gốm cổ rêu phong Bát Tràng. Dưới sự chỉ bảo tận tình của Nghệ nhân Ưu tú Nguyễn Văn An, bạn sẽ được tự tay nhào nặn khối đất sét dẻo quánh, điều khiển lực chân và ngón tay trên bàn xoay thủ công để tạo nên sản phẩm của riêng mình. Sau đó, bạn sẽ học cách vẽ men lam cổ và tác phẩm sẽ được nung chín gửi tận tay bạn.",
                ExperienceSteps = "1. Tham quan lò nung cổ và nghe nghệ nhân chia sẻ câu chuyện đất & lửa.\n2. Hướng dẫn kỹ thuật đặt tâm và chuốt gốm trên bàn xoay tay.\n3. Khách tự tay vuốt 2-3 sản phẩm (ly, bình hoa, chén nhỏ).\n4. Khắc tên, vẽ hoa văn men lam theo phong cách truyền thống.\n5. Xưởng hỗ trợ sấy, tráng men và nung trong lò 1.250°C.",
                TakeHomeProduct = "1 chiếc cốc hoặc bình hoa gốm men lam hoàn thiện sau nung (được đóng gói hộp quà chống sốc an toàn).",
                TargetAudience = "Phù hợp cho mọi lứa tuổi, cặp đôi, gia đình có trẻ nhỏ và du khách quốc tế yêu văn hóa truyền thống.",
                PreparationGuide = "Mặc trang phục thoải mái, dễ giặt (xưởng có trang bị tạp dề vải thô vintage). Móng tay cắt gọn gàng để dễ thao tác vuốt đất sét.",
                IncludedServices = "Nguyên liệu đất sét trắng không giới hạn, tạp dề bảo hộ, trà mạn hoa cúc & bánh đậu xanh Bát Tràng, đóng gói sản phẩm hoàn thiện.",
                BasePrice = 350000m,
                DurationMinutes = 150,
                MinParticipants = 1,
                MaxParticipants = 15,
                LocationAddress = "Số 18, Ngõ Cổ Bát Tràng, Gia Lâm, Hà Nội",
                CoverImageUrl = "https://images.unsplash.com/photo-1578749556568-bc2c40e68b61?auto=format&fit=crop&w=1000&q=80",
                Status = WorkshopStatus.Published,
                IsFeatured = true,
                AverageRating = 4.9,
                ReviewCount = 28
            };

            var ws2 = new Workshop
            {
                HostId = hostUser2!.Id,
                CategoryId = catLua!.Id,
                VillageId = vanPhucVillage?.Id,
                Title = "Hành trình tơ tằm Vạn Phúc: Ươm kén, dệt lụa & nhuộm màu cỏ cây",
                Slug = "hanh-trinh-to-tam-van-phuc-uom-ken-det-lua",
                ShortDescription = "Khám phá bí mật sợi tơ tiến vua, trực tiếp ngồi dệt trên khung cửi cổ và tự tay nhuộm khăn lụa tơ tằm bằng lá cây thiên nhiên.",
                FullDescription = "Vạn Phúc không chỉ có phố đèn lồng rực rỡ mà còn ẩn chứa những câu chuyện kỳ diệu về loài sâu tằm nhả tơ. Tại cơ sở Mai Loan, nghệ nhân Trần Thị Mai sẽ hướng dẫn bạn từ bước đảo kén tằm trong nồi nước sôi thơm mùi lá dâu đến cách dệt những đường thoi bay nhịp nhàng. Điểm nhấn đặc biệt là bạn sẽ tự tay nhuộm một chiếc khăn lụa tơ tằm 100% bằng lá bàng, vỏ cây tô mộc và củ nâu.",
                ExperienceSteps = "1. Giới thiệu vòng đời kén tằm và trải nghiệm kéo sợi tơ nguyên bản.\n2. Thử sức gạt thoi dệt lụa trên khung cửi gỗ truyền thống.\n3. Học công thức pha chế màu nhuộm hữu cơ từ thảo mộc làng quê.\n4. Thực hiện kỹ thuật buộc thắt Shibori và nhuộm khăn lụa.\n5. Giặt xả nước sạch, là phẳng và đóng gói khăn lụa mang về.",
                TakeHomeProduct = "1 chiếc khăn quàng cổ lụa tơ tằm Vạn Phúc cao cấp do chính tay bạn nhuộm màu.",
                TargetAudience = "Yêu thích thời trang bền vững, thủ công mỹ nghệ, hội nhóm bạn bè và gia đình.",
                PreparationGuide = "Trang phục gọn gàng, tránh mặc đồ trắng tinh để không bị bắn nước nhuộm thảo mộc.",
                IncludedServices = "1 phôi khăn lụa tơ tằm 100%, nguyên liệu màu nhuộm thảo dược, trà sen Tây Hồ thưởng thức trong xưởng.",
                BasePrice = 450000m,
                DurationMinutes = 180,
                MinParticipants = 1,
                MaxParticipants = 12,
                LocationAddress = "Số 68 Phố Lụa Vạn Phúc, Hà Đông, Hà Nội",
                CoverImageUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?auto=format&fit=crop&w=1000&q=80",
                Status = WorkshopStatus.Published,
                IsFeatured = true,
                AverageRating = 4.8,
                ReviewCount = 19
            };

            var ws3 = new Workshop
            {
                HostId = hostUser1!.Id,
                CategoryId = catMay!.Id,
                VillageId = (await context.Villages.FirstOrDefaultAsync(v => v.Slug == "lang-may-tre-dan-phu-vinh"))?.Id,
                Title = "Nghệ thuật đan nan tre: Tạo khay hoa sen & giỏ quả mộc mạc",
                Slug = "nghe-thuat-dan-nan-tre-phu-vinh",
                ShortDescription = "Cảm nhận sự dẻo dai của tre nứa xứ Đoài và tự tay đan chiếc khay hoa sen mang hơi thở thôn quê ấm áp.",
                FullDescription = "Một trải nghiệm thiền tịnh cùng tre nứa làng Phú Vinh. Bạn sẽ được học cách cầm dao chẻ nan, chuốt bóng từng sợi cật tre dẻo dai rồi đan gài khéo léo theo hoa văn cánh sen truyền thống.",
                ExperienceSteps = "1. Tìm hiểu các loại tre, mây và công nghệ hun khói lá chống ẩm mốc.\n2. Tập chẻ và vót nan tre mảnh mượt.\n3. Bắt nan tạo đáy và đan thân khay hoa sen theo mẫu.\n4. Bẻ miệng giỏ, cuộn viền mây chắc chắn.",
                TakeHomeProduct = "1 khay trà hoặc giỏ mây tre đan hoa sen đường kính 25cm.",
                TargetAudience = "Người thích trang trí nhà cửa tối giản (Eco/Wabi-sabi), yêu thiên nhiên.",
                PreparationGuide = "Xưởng chuẩn bị sẵn găng tay và dụng cụ bấm nan chuyên dụng.",
                IncludedServices = "Nan tre mây đã qua xử lý, trà mộc hoa nhài, hướng dẫn 1-kèm-1.",
                BasePrice = 320000m,
                DurationMinutes = 120,
                MinParticipants = 1,
                MaxParticipants = 10,
                LocationAddress = "Làng nghề Phú Vinh, Phú Nghĩa, Chương Mỹ, Hà Nội",
                CoverImageUrl = "https://images.unsplash.com/photo-1596040033229-a9821ebd058d?auto=format&fit=crop&w=1000&q=80",
                Status = WorkshopStatus.Published,
                IsFeatured = true,
                AverageRating = 4.9,
                ReviewCount = 15
            };

            context.Workshops.AddRange(ws1, ws2, ws3);
            await context.SaveChangesAsync();

            // Seed Workshop Images
            context.WorkshopImages.AddRange(
                new WorkshopImage { WorkshopId = ws1.Id, ImageUrl = ws1.CoverImageUrl, IsCover = true, DisplayOrder = 1, Caption = "Chuốt gốm trên bàn xoay Bát Tràng" },
                new WorkshopImage { WorkshopId = ws1.Id, ImageUrl = "https://images.unsplash.com/photo-1565193566173-7a0ee3dbe261?auto=format&fit=crop&w=800&q=80", IsCover = false, DisplayOrder = 2, Caption = "Học vẽ men lam thủ công" },
                new WorkshopImage { WorkshopId = ws1.Id, ImageUrl = "https://images.unsplash.com/photo-1581783342308-f792dbdd27c5?auto=format&fit=crop&w=800&q=80", IsCover = false, DisplayOrder = 3, Caption = "Sản phẩm gốm mộc chờ vào lò" },
                new WorkshopImage { WorkshopId = ws2.Id, ImageUrl = ws2.CoverImageUrl, IsCover = true, DisplayOrder = 1, Caption = "Khung dệt lụa cổ Vạn Phúc" },
                new WorkshopImage { WorkshopId = ws2.Id, ImageUrl = "https://images.unsplash.com/photo-1520006403909-838d6b92c22e?auto=format&fit=crop&w=800&q=80", IsCover = false, DisplayOrder = 2, Caption = "Nhuộm khăn lụa bằng thảo mộc thiên nhiên" },
                new WorkshopImage { WorkshopId = ws3.Id, ImageUrl = ws3.CoverImageUrl, IsCover = true, DisplayOrder = 1, Caption = "Đan nan mây hoa sen thủ công" }
            );

            // Seed Schedules & Slots for the next 30 days
            var today = DateTime.UtcNow.Date;
            for (int i = 0; i < 30; i++)
            {
                var scheduleDate = today.AddDays(i);

                // For ws1
                var sch1 = new WorkshopSchedule { WorkshopId = ws1.Id, Date = scheduleDate, IsActive = true };
                sch1.Slots.Add(new Slot { Schedule = sch1, StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(11, 0, 0), MaxCapacity = 15, AvailableCapacity = 15 });
                sch1.Slots.Add(new Slot { Schedule = sch1, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(16, 30, 0), MaxCapacity = 15, AvailableCapacity = 15 });
                context.WorkshopSchedules.Add(sch1);

                // For ws2
                var sch2 = new WorkshopSchedule { WorkshopId = ws2.Id, Date = scheduleDate, IsActive = true };
                sch2.Slots.Add(new Slot { Schedule = sch2, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(12, 0, 0), MaxCapacity = 12, AvailableCapacity = 12 });
                sch2.Slots.Add(new Slot { Schedule = sch2, StartTime = new TimeSpan(14, 30, 0), EndTime = new TimeSpan(17, 30, 0), MaxCapacity = 12, AvailableCapacity = 12 });
                context.WorkshopSchedules.Add(sch2);

                // For ws3
                var sch3 = new WorkshopSchedule { WorkshopId = ws3.Id, Date = scheduleDate, IsActive = true };
                sch3.Slots.Add(new Slot { Schedule = sch3, StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(10, 30, 0), MaxCapacity = 10, AvailableCapacity = 10 });
                sch3.Slots.Add(new Slot { Schedule = sch3, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(16, 0, 0), MaxCapacity = 10, AvailableCapacity = 10 });
                context.WorkshopSchedules.Add(sch3);
            }
            await context.SaveChangesAsync();
        }

        // 8. Seed Curated Trips & Itineraries
        var firstWorkshop = await context.Workshops.FirstOrDefaultAsync(w => w.Slug == "tu-tay-vuot-gom-bat-trang-va-ve-men-lam-co");
        var secondWorkshop = await context.Workshops.FirstOrDefaultAsync(w => w.Slug == "hanh-trinh-to-tam-van-phuc-uom-ken-det-lua");

        if (!await context.Trips.AnyAsync())
        {
            var trip1 = new Trip
            {
                Title = "Hành trình 1 ngày: Chạm vào hồn gốm Bát Tràng & ẩm thực ven sông Hồng",
                Slug = "hanh-trinh-1-ngay-cham-vao-hon-gom-bat-trang",
                ShortDescription = "Trải nghiệm kết hợp hoàn hảo: Ăn sáng canh măng mực tiến vua, vuốt gốm cùng nghệ nhân và check-in lò bầu cổ độc nhất vô nhị.",
                FullDescription = "Một ngày rời xa khói bụi thành phố, thong dong men theo triền đê sông Hồng để trở về với làng gốm Bát Tràng. Lịch trình được thiết kế nhịp nhàng kết hợp tinh hoa ẩm thực cổ truyền và trải nghiệm tự tay tạo tác sản phẩm gốm sứ.",
                VillageId = batTrangVillage?.Id,
                WorkshopId = firstWorkshop?.Id,
                DurationDescription = "1 ngày (08:00 - 16:30)",
                EstimatedCost = 650000m,
                CoverImageUrl = "https://images.unsplash.com/photo-1578749556568-bc2c40e68b61?auto=format&fit=crop&w=1200&q=80",
                IsFeatured = true,
                DisplayOrder = 1
            };

            trip1.Itineraries.Add(new TripItinerary
            {
                Trip = trip1,
                StopOrder = 1,
                TimeSlot = "08:00 - 09:15",
                Title = "Ăn sáng đặc sản Bát Tràng: Canh măng mực & xôi vò chè đường",
                LocationName = "Quán cổ cổng đình Bát Tràng",
                Description = "Thưởng thức món canh măng mực tiến vua trứ danh chỉ có tại cỗ truyền thống Bát Tràng, nạp năng lượng cho ngày khám phá.",
                IconClass = "bi-cup-hot"
            });

            trip1.Itineraries.Add(new TripItinerary
            {
                Trip = trip1,
                StopOrder = 2,
                TimeSlot = "09:30 - 12:00",
                Title = "Trọng tâm: Tham gia Workshop vuốt gốm & vẽ men lam tại Xưởng Nghệ Nhân An",
                LocationName = "Xưởng gốm An Bát Tràng",
                Description = "Ngồi bên bàn xoay tay, cảm nhận từng thớ đất sét và vẽ men lam lên chính tác phẩm của bạn dưới sự hướng dẫn của nghệ nhân.",
                IconClass = "bi-palette"
            });

            trip1.Itineraries.Add(new TripItinerary
            {
                Trip = trip1,
                StopOrder = 3,
                TimeSlot = "12:15 - 13:45",
                Title = "Bữa trưa cơm quê thanh đạm bên giàn hoa giấy ven sông",
                LocationName = "Nhà hàng vườn Gốm Xưa",
                Description = "Nghỉ ngơi, thưởng thức cá bống kho tiêu, canh rau tập tàng nấu cua đồng và tráng miệng trà sen ướp mạn.",
                IconClass = "bi-egg-fried"
            });

            trip1.Itineraries.Add(new TripItinerary
            {
                Trip = trip1,
                StopOrder = 4,
                TimeSlot = "14:00 - 16:30",
                Title = "Check-in Bảo tàng gốm Bát Tràng & khám phá lò bầu cổ 100 năm tuổi",
                LocationName = "Trung tâm Tinh hoa Làng nghề Việt & Lò Bầu Cổ",
                Description = "Chiêm ngưỡng kiến trúc uốn lượn 7 bàn xoay khổng lồ độc đáo và dạo bước qua từng ngõ gạch cổ rêu phong để mua quà lưu niệm.",
                IconClass = "bi-camera"
            });

            var trip2 = new Trip
            {
                Title = "Dạo bước Vạn Phúc: Hương lụa ngàn năm, thưởng trà sen & ẩm thực phố cổ",
                Slug = "dao-buoc-van-phuc-huong-lua-ngan-nam",
                ShortDescription = "Lộ trình buổi sáng đậm chất thơ: Check-in con đường ô lụa sắc màu, dệt khăn lụa tơ tằm và thưởng thức chả cá Hà Đông trứ danh.",
                FullDescription = "Khám phá làng lụa cổ nhất miền Bắc trong một buổi sáng đầy ắp trải nghiệm xúc giác và thị giác. Từ âm vang lách cách của thoi đưa đến sự mềm mại của dải lụa vừa nhấc khỏi nồi nước nhuộm thảo mộc.",
                VillageId = vanPhucVillage?.Id,
                WorkshopId = secondWorkshop?.Id,
                DurationDescription = "Nửa ngày (08:30 - 13:30)",
                EstimatedCost = 550000m,
                CoverImageUrl = "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?auto=format&fit=crop&w=1200&q=80",
                IsFeatured = true,
                DisplayOrder = 2
            };

            trip2.Itineraries.Add(new TripItinerary
            {
                Trip = trip2,
                StopOrder = 1,
                TimeSlot = "08:30 - 09:15",
                Title = "Dạo bước check-in con đường ô lụa Vạn Phúc & uống nước chè xanh quán đình",
                LocationName = "Cổng làng & Phố lụa Vạn Phúc",
                Description = "Ngắm nhìn hàng ngàn chiếc ô rực rỡ sắc màu đung đưa dưới nắng và tìm hiểu kiến trúc đình thờ tổ nghề.",
                IconClass = "bi-geo-alt"
            });

            trip2.Itineraries.Add(new TripItinerary
            {
                Trip = trip2,
                StopOrder = 2,
                TimeSlot = "09:30 - 12:00",
                Title = "Workshop dệt lụa tơ tằm & tự tay nhuộm khăn thảo mộc",
                LocationName = "Cơ sở lụa cổ truyền Mai Loan",
                Description = "Ngồi bên khung cửi, thử cảm giác đưa thoi và tự tay nhuộm chiếc khăn lụa tơ tằm 100% bằng lá cây tự nhiên.",
                IconClass = "bi-flower2"
            });

            trip2.Itineraries.Add(new TripItinerary
            {
                Trip = trip2,
                StopOrder = 3,
                TimeSlot = "12:15 - 13:30",
                Title = "Bữa trưa chả cá lăng nướng than hoa Hà Đông thơm nức mũi",
                LocationName = "Quán Chả Cá Phố Lụa",
                Description = "Thưởng thức chả cá lăng vàng ruộm ăn kèm thì là tươi, bún rối và mắm tôm pha chanh ớt sủi bọt đậm đà.",
                IconClass = "bi-cup-straw"
            });

            context.Trips.AddRange(trip1, trip2);
            await context.SaveChangesAsync();
        }

        // 9. Seed Commission Rules (BR_CM_01)
        if (!await context.CommissionRules.AnyAsync())
        {
            context.CommissionRules.Add(new CommissionRule
            {
                Name = "Chính sách hoa hồng CraftTrip 2026",
                SmallGroupRate = 0.12m, // 12%
                LargeGroupRate = 0.15m, // 15%
                GroupSizeThreshold = 5,  // Configurable threshold (>= 5 is large group)
                IsActive = true
            });
            await context.SaveChangesAsync();
        }

        // 10. Seed System Settings
        if (!await context.SystemSettings.AnyAsync())
        {
            context.SystemSettings.AddRange(
                new SystemSetting { Key = "SiteName", Value = "CraftTrip — Tinh hoa Làng nghề Việt", Description = "Tên hiển thị thương hiệu sàn", Category = "General" },
                new SystemSetting { Key = "ContactEmail", Value = "hotro@crafttrip.vn", Description = "Email hỗ trợ khách hàng", Category = "General" },
                new SystemSetting { Key = "ContactHotline", Value = "1900 6868", Description = "Hotline tổng đài 24/7", Category = "General" },
                new SystemSetting { Key = "CommissionSmallGroup", Value = "0.12", Description = "Tỷ lệ hoa hồng nhóm nhỏ (< 5 người)", Category = "Financial" },
                new SystemSetting { Key = "CommissionLargeGroup", Value = "0.15", Description = "Tỷ lệ hoa hồng nhóm lớn (>= 5 người)", Category = "Financial" },
                new SystemSetting { Key = "CommissionGroupThreshold", Value = "5", Description = "Ngưỡng số lượng khách phân biệt nhóm nhỏ/lớn", Category = "Financial" },
                new SystemSetting { Key = "PendingBookingExpirationMinutes", Value = "15", Description = "Thời gian hết hạn đơn giữ chỗ Pending (phút)", Category = "Booking" },
                new SystemSetting { Key = "CancellationRefundHours", Value = "48", Description = "Số giờ tối thiểu trước ca để được hủy hoàn tiền 100%", Category = "Booking" },
                new SystemSetting { Key = "PayoutEligibilityHours", Value = "24", Description = "Số giờ sau khi ca kết thúc để Host đủ điều kiện rút tiền", Category = "Financial" }
            );
            await context.SaveChangesAsync();
        }

        // 11. Seed CMS Homepage Content
        if (!await context.ContentPages.AnyAsync(p => p.PageKey == "home"))
        {
            var homePage = new ContentPage
            {
                PageKey = "home",
                Title = "CraftTrip — Nền tảng Trải nghiệm Làng nghề & Workshop Thủ công Việt Nam",
                MetaDescription = "Khám phá văn hóa làng nghề truyền thống Việt Nam, đặt workshop trực tiếp với nghệ nhân bản địa.",
                IsPublished = true
            };

            homePage.Sections.Add(new ContentSection
            {
                Page = homePage,
                SectionKey = "hero",
                Title = "Chạm vào tinh hoa làng nghề Việt",
                Subtitle = "Khám phá những làng nghề truyền thống, gặp gỡ nghệ nhân và tự tay tạo nên những tác phẩm mang dấu ấn Việt Nam.",
                PrimaryCtaText = "Khám phá trải nghiệm",
                PrimaryCtaUrl = "/Workshop",
                SecondaryCtaText = "Trở thành Host",
                SecondaryCtaUrl = "/Host/Onboarding",
                BackgroundImageUrl = "https://images.unsplash.com/photo-1578749556568-bc2c40e68b61?auto=format&fit=crop&w=1600&q=80",
                DisplayOrder = 1,
                IsActive = true
            });

            homePage.Sections.Add(new ContentSection
            {
                Page = homePage,
                SectionKey = "how_it_works",
                Title = "Hành trình trải nghiệm tại CraftTrip",
                Subtitle = "Đơn giản, minh bạch và gắn kết chân thực cùng nghệ nhân",
                ContentHtml = "<div class='row g-4'><div class='col-md-4 text-center'><div class='step-icon mb-3'><i class='bi bi-compass fs-1 text-primary'></i></div><h5>1. Tìm kiếm & Chọn ca</h5><p class='text-muted'>Chọn làng nghề, thể loại thủ công và khung giờ phù hợp với lịch trình của bạn.</p></div><div class='col-md-4 text-center'><div class='step-icon mb-3'><i class='bi bi-qr-code-scan fs-1 text-primary'></i></div><h5>2. Nhận vé điện tử QR</h5><p class='text-muted'>Thanh toán an toàn qua VNPay và nhận vé QR tức thì trong tài khoản và email.</p></div><div class='col-md-4 text-center'><div class='step-icon mb-3'><i class='bi bi-heart fs-1 text-primary'></i></div><h5>3. Trực tiếp làm đồ thủ công</h5><p class='text-muted'>Đến xưởng, gặp gỡ nghệ nhân và tự hào mang tác phẩm do chính tay bạn làm về nhà.</p></div></div>",
                DisplayOrder = 2,
                IsActive = true
            });

            homePage.Sections.Add(new ContentSection
            {
                Page = homePage,
                SectionKey = "become_host",
                Title = "Bạn là nghệ nhân hay chủ cơ sở thủ công làng nghề?",
                Subtitle = "Cùng CraftTrip đưa câu chuyện nghề truyền thống của bạn đến với hàng ngàn du khách trong và ngoài nước.",
                PrimaryCtaText = "Đăng ký mở xưởng ngay",
                PrimaryCtaUrl = "/Host/Onboarding",
                BackgroundImageUrl = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=1200&q=80",
                DisplayOrder = 3,
                IsActive = true
            });

            homePage.Sections.Add(new ContentSection
            {
                Page = homePage,
                SectionKey = "footer",
                Title = "CraftTrip — Giữ gìn hồn quê Việt qua từng nét chạm",
                Subtitle = "Hệ sinh thái kết nối di sản văn hóa truyền thống với thế hệ tương lai.",
                ContentHtml = "© 2026 CraftTrip. Bản quyền thuộc về Nhóm dự án Làng nghề Việt.",
                DisplayOrder = 4,
                IsActive = true
            });

            context.ContentPages.Add(homePage);
            await context.SaveChangesAsync();
        }

        // 12. Seed A Completed Demo Booking with Review for Guest
        if (guestUser != null && firstWorkshop != null && !await context.Bookings.AnyAsync(b => b.GuestId == guestUser.Id))
        {
            var yesterdaySlot = await context.Slots.Include(s => s.Schedule).FirstOrDefaultAsync(s => s.Schedule!.WorkshopId == firstWorkshop.Id);
            if (yesterdaySlot != null)
            {
                var demoBooking = new Booking
                {
                    BookingCode = "CT-DEMO-9988",
                    GuestId = guestUser.Id,
                    WorkshopId = firstWorkshop.Id,
                    SlotId = yesterdaySlot.Id,
                    Quantity = 2,
                    UnitPrice = 350000m,
                    TotalAmount = 700000m,
                    CommissionRate = 0.12m,
                    PlatformFee = 84000m,
                    HostEarnings = 616000m,
                    Status = BookingStatus.Completed,
                    CustomerName = guestUser.FullName,
                    CustomerEmail = guestUser.Email!,
                    CustomerPhone = guestUser.PhoneNumber ?? "0905112233",
                    AttendanceStatus = AttendanceStatus.CheckedIn,
                    CheckedInAt = DateTime.UtcNow.AddDays(-1),
                    CheckedInByHostId = hostUser1?.Id,
                    ExpiresAt = DateTime.UtcNow.AddDays(-2),
                    Ticket = new BookingTicket
                    {
                        TicketCode = "TCK-DEMO-9988-OK",
                        QrCodeData = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=",
                        IsRedeemed = true,
                        RedeemedAt = DateTime.UtcNow.AddDays(-1)
                    }
                };

                demoBooking.Payments.Add(new Payment
                {
                    Booking = demoBooking,
                    Amount = 700000m,
                    PaymentMethod = "VNPay",
                    Status = PaymentStatus.Success,
                    TransactionNo = "VNP14889922",
                    BankCode = "NCB",
                    PayDate = DateTime.UtcNow.AddDays(-2),
                    ResponseCode = "00"
                });

                demoBooking.Review = new Review
                {
                    Booking = demoBooking,
                    WorkshopId = firstWorkshop.Id,
                    GuestId = guestUser.Id,
                    Rating = 5,
                    Comment = "Trải nghiệm vuốt gốm tuyệt vời vượt ngoài mong đợi! Bác An hướng dẫn cực kỳ ân cần, xưởng gốm cổ kính mát mẻ. Chiếc cốc gốm nung xong mang về men sáng bóng rất đẹp.",
                    HostReply = "Cảm ơn cháu Trúc nhiều nhé! Chúc cháu luôn vui vẻ và có dịp lại ghé xưởng chơi với bác.",
                    RepliedAt = DateTime.UtcNow.AddHours(-12)
                };

                context.Bookings.Add(demoBooking);

                // Add Host Payout for this booking (BR_PO_02)
                context.HostPayouts.Add(new HostPayout
                {
                    HostId = hostUser1!.Id,
                    Booking = demoBooking,
                    Amount = 616000m,
                    EligibleAt = DateTime.UtcNow.AddHours(-5), // Session ended > 24 hours ago, now eligible!
                    Status = PayoutStatus.Eligible,
                    PayoutReference = "PO-202611-001"
                });

                await context.SaveChangesAsync();
            }
        }
    }
}
