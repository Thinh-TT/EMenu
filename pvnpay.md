# VNPay Sandbox Debug Checklist

## 1. Cau hinh bat buoc

- `VNPay:TmnCode` co gia tri.
- `VNPay:HashSecret` co gia tri.
- `VNPay:Url` dung sandbox: `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html`.
- `VNPay:ReturnUrl` phai khop dung URL app dang chay (scheme + host + port + path).

## 2. Kiem tra local profile

- Neu chay profile `https`, `ReturnUrl` phai tro ve port HTTPS (vi du `https://localhost:7085/Payment/VNPayReturn`).
- Neu chay profile `http`, `ReturnUrl` phai dung port HTTP tuong ung.
- Khong dung sai port/khac scheme vi VNPay se redirect sai endpoint.

## 3. Kiem tra chu ky callback

- Callback phai co `vnp_SecureHash`.
- He thong verify hash tu toan bo tham so `vnp_*` (tru `vnp_SecureHash`, `vnp_SecureHashType`).
- Neu hash sai: he thong khong ghi nhan thanh toan.

## 4. Kiem tra trang thai giao dich

- Thanh cong chi khi:
  - `vnp_ResponseCode == 00`
  - `vnp_TransactionStatus == 00`
- Trang thai khac: hien trang fail va khong ghi payment.

## 5. Kiem tra TxnRef va idempotency

- `TxnRef` co format parse duoc: chua `sessionId`, `orderId`, timestamp/random.
- Callback duplicate khong tao invoice/payment trung (idempotent theo `orderId`).

## 6. Kiem tra du lieu sau thanh toan thanh cong

- Co `Invoice` moi cho `Order`.
- Co `Payment` method `VNPay`.
- `Order` chuyen `Completed`.
- `OrderSession` dong (`Status=0`) va ban tra ve `Available`.

## 7. Log can theo doi

- Init payment: sessionId, orderId, amount, txnRef.
- Callback success/fail: txnRef, responseCode, transactionStatus, transactionNo.
- Loi hash/parse/config: thong tin nguyen nhan cu the.

* Da hoan thanh
