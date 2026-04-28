# Ke Hoach Tinh Nang Chuyen Doi Ngon Ngu Anh/Viet (Header Toggle)

## Summary
- Muc tieu: them nut chuyen ngon ngu tren header de doi UI giua tieng Anh va tieng Viet.
- Pham vi dot 1: ha tang localization + dich core pages da chot (`Layout`, `Home`, `Dashboard`, `Table`, `Reservation`, `Checkout`, `Payment`, `Login`).
- Luu lua chon ngon ngu bang cookie + returnUrl de nguoi dung o lai dung trang hien tai sau khi chuyen.
- Backend error/API message giu nguyen tieng Anh o dot nay.

## Implementation Changes
- Thiet lap localization trong `Program.cs`:
  - Bat `AddLocalization(ResourcesPath = "Resources")`.
  - Bat `AddControllersWithViews().AddViewLocalization().AddDataAnnotationsLocalization()`.
  - Cau hinh `RequestLocalizationOptions` voi `en-US`, `vi-VN`; default `en-US`.
  - Dang ky provider theo thu tu: cookie truoc, sau do querystring (de ho tro debug khi can).
- Them endpoint doi ngon ngu:
  - Tao `LocalizationController` voi action `SetLanguage(culture, returnUrl)`.
  - Dung `CookieRequestCultureProvider.MakeCookieValue(...)` de ghi cookie.
  - Redirect bang `LocalRedirect(returnUrl)` (fallback `/` neu returnUrl khong hop le) de tranh open-redirect.
- Cap nhat header trong `_Layout.cshtml`:
  - Them nut/nhom nut `EN | VI` o khu vuc user-nav.
  - Moi nut goi `Localization/SetLanguage` va truyen `returnUrl` la URL hien tai.
  - Doi `<html lang="en">` thanh bind theo `CurrentUICulture` de phan anh ngon ngu thuc te.
- Chuan hoa nguon text UI theo resource:
  - Dung `IViewLocalizer` cho text trong cac view thuoc pham vi core pages.
  - Tao resource `.resx` cho `en-US` va `vi-VN` theo cau truc view-localization.
  - Khong hardcode chuoi moi trong view; dung key ro nghia (vi du `Nav.Operations`, `Checkout.Title`, `Payment.SuccessTitle`).
- Localize message tu JS o core pages:
  - Voi cac trang co alert/message JS (dac biet `Table`, `Reservation`, `Checkout/Payment`), inject dictionary localized vao view (qua `data-*` hoac object JS global tu Razor).
  - JS doc message theo key thay vi string hardcode tieng Anh.
- Khong doi contract API nghiep vu hien co:
  - Chi them endpoint doi culture cho UI.
  - Khong sua text `BadRequest/InvalidOperationException` o controller/service trong dot nay.

## Public Interfaces / Contract Impact
- Them endpoint moi cho UI:
  - `GET /Localization/SetLanguage?culture={en-US|vi-VN}&returnUrl={local-url}`
- Khong thay doi endpoint business hien tai (`/api/*`, `/Payment/*`, ...).

## Test Plan
- Unit/integration:
  - `SetLanguage` ghi dung cookie culture va redirect ve `returnUrl`.
  - `returnUrl` khong hop le phai fallback an toan (khong redirect ra ngoai domain local).
- UI smoke test:
  - Bam `VI` tren header: text o cac core pages doi sang tieng Viet.
  - Bam `EN`: quay lai tieng Anh.
  - Chuyen trang van giu ngon ngu da chon (cookie persistence).
  - Refresh trang/ngat phien van giu ngon ngu.
- Regression checks:
  - Dropdown/menu header van hoat dong binh thuong.
  - Cac luong chinh `Table -> Reservation -> Checkout -> Payment result` hien thi dung ngon ngu UI.

## Assumptions / Defaults
- Default culture giu la English (`en-US`) de khong pha hanh vi hien tai.
- Chi ho tro 2 ngon ngu trong dot nay: `en-US`, `vi-VN`.
- Chi localize UI thuoc pham vi core pages; message backend/API giu tieng Anh.
- Khong them bang DB, khong luu ngon ngu theo user profile o giai doan nay.
