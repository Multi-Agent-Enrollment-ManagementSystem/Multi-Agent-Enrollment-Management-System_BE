# 🔧 FIX FOR GEMINI API ERROR

## 🎯 Vấn đề
```
{
  "message": "AI service is temporarily unavailable. Please try again later."
}
```

## ✅ Sửa chữa đã áp dụng

### **1. Change Model Name** ✓
**File:** `MAEMS.API/appsettings.json`

```json
// TRƯỚC
"ModelName": "gemini-1.5-flash"

// SAU
"ModelName": "gemini-pro"
```

**Lý do:** `gemini-1.5-flash` chưa public đầy đủ hoặc API key không có quyền.

---

### **2. Improve Error Logging** ✓
**File:** `MAEMS.Infrastructure/Services/GeminiService.cs`

Thêm:
- URL logging (xem exact URL được gọi)
- Request body logging (xem data gửi)
- Response logging (xem server trả lại gì)
- Better error messages

---

### **3. Better Exception Handling** ✓
**File:** `MAEMS.API/Controllers/ChatBoxController.cs`

Thêm:
- IndexOutOfRangeException (khi API response format sai)
- InvalidOperationException (khi config sai)
- Detailed error messages

---

## 🚀 Cách test

### **Step 1: Run application**
```bash
dotnet run
```

### **Step 2: Open Output window**
```
Visual Studio → View → Output → Show output from "Debug"
```

### **Step 3: Make API call**
```http
POST /api/chatbox/ask
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "question": "Xin chào"
}
```

### **Step 4: Check logs**
```
Logs will show:
- Gemini API URL
- Request body
- Response from server
- Exact error if any
```

---

## 🔍 Common Issues & Solutions

### ❌ Issue: "Invalid API key"
**Solution:**
```
1. Check API key in appsettings.json
2. Copy from: https://ai.google.dev/
3. Make sure it's the right one
4. Regenerate if needed
```

### ❌ Issue: "Model not found"
**Solution:**
```
Available models:
✓ gemini-pro
✓ gemini-pro-vision
✗ gemini-1.5-flash (not yet)
```

### ❌ Issue: "Quota exceeded"
**Solution:**
```
Free tier has limits:
- Rate: 60 requests/minute
- Daily: 1500 requests
Check: https://ai.google.dev/
```

---

## 📝 Changes Summary

| File | Change | Status |
|------|--------|--------|
| `appsettings.json` | Model: gemini-pro | ✅ Done |
| `GeminiService.cs` | Add detailed logging | ✅ Done |
| `ChatBoxController.cs` | Better error handling | ✅ Done |

---

## ✅ Next Steps

1. Run `dotnet build` to confirm no errors
2. Start application with `dotnet run`
3. Test `/api/chatbox/ask` endpoint
4. Check Output window for detailed logs
5. If still error → Copy logs here for analysis

---

**Built:** ✅  
**Ready to test:** ✅  
**No additional files created:** ✅
