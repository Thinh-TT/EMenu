# Ke Hoach Transfer / Gop Ban

## Summary

- Bo sung nghiep vu chuyen ban va gop ban tren luong Session/Order hien tai.
- Giua nguyen flow thanh toan hien tai.
- Ke hoach nay du chi tiet de implement truc tiep.

## Quy tac nghiep vu

- Transfer:
  - Nguon phai `Occupied`.
  - Dich chi duoc `Available`.
  - Chuyen toan bo order chua invoiced sang session moi tai ban dich.
  - Dong session nguon, giai phong ban nguon, danh dau ban dich `Occupied`.
- Merge:
  - Nguon phai `Occupied`.
  - Dich chi nhan `Available` hoac `Occupied`, khong nhan `Reserved`.
  - Neu dich `Occupied`: gop vao session dich, giu customer cua ban dich.
  - Neu dich `Available`: tao session dich moi (customer lay tu session nguon), roi gop order.
  - Sau gop phai dong session nguon va giai phong ban nguon.
- Rule chan:
  - Khong cho `source == target`.
  - Neu session lien quan co order da invoiced thi chan thao tac.

## API va contract

- Them API:
  - `POST /api/session/transfer`
  - `POST /api/session/merge`
- Request body toi thieu cho ca hai:
  - `SourceTableId`
  - `TargetTableId`
  - `Actor`
- Audit log bang `ILogger`, log day du:
  - thoi diem
  - source/target table
  - source/target session
  - so order da chuyen
  - ket qua thanh cong/that bai va ly do

## Service / Repository can mo rong

- `SessionService`:
  - Them `TransferTable(...)`
  - Them `MergeTable(...)`
  - Bao dam transaction cho cac update nhieu bang.
- `IOrderRepository` + implementation:
  - Kiem tra session co order invoiced.
  - Batch reassign `OrderSessionID` cho order chua invoiced.
- Tiep tuc su dung `UnitOfWork + Transaction` de tranh lech trang thai.

## UI Table

- Mo rong man hinh `Table`:
  - Them action `Transfer` va `Merge` cho ban dang `Occupied`.
  - Mo modal chon ban dich theo rule tung thao tac.
  - Sau thao tac thanh cong, reload trang thai ban.
- Khong trien khai co che forward session/QR cu sau merge.

## Test cases

- Unit tests:
  - Transfer thanh cong khi nguon occupied, dich available, khong invoiced.
  - Transfer bi chan khi dich khong available, source=target, hoac co invoiced.
  - Merge vao dich occupied: giu customer dich, dồn order dung session dich.
  - Merge vao dich available: tao session dich moi roi dồn order.
  - Merge bi chan khi dich reserved hoac co invoiced.
- Integration tests:
  - Transfer API thanh cong va cap nhat dung table/session/order.
  - Merge API (target occupied) thanh cong va dong session nguon.
  - Merge API (target reserved) bi chan, du lieu nguon khong bi doi.

* Da hoan thanh
