# Internal News (Tin Nội Bộ) - HTTP 500 Error Fix

## Problem Summary
When creating or viewing internal news (tin nội bộ), the application was throwing an HTTP 500 error on the Detail page after creation.

### Root Cause
**NullReferenceException** when accessing `UserData.RoleCode` before `UserData` was fully initialized or in certain edge cases where `UserData` could be null.

The issue occurred in several files in `/crmHuman/Pages/InternalNews/`:
- Properties using `UserData.RoleCode` without null-safe checks
- These properties were being accessed in views before `GetInfoUser()` could execute

## Files Fixed

### 1. **Detail.cshtml.cs** (3 changes)
**Lines 18-19:** Fixed property definitions
```csharp
// BEFORE
public bool CanEdit => (Permision.Edit ?? false) || UserData.RoleCode == "1";
public bool CanDelete => (Permision.Delete ?? false) || UserData.RoleCode == "1";

// AFTER
public bool CanEdit => (Permision.Edit ?? false) || (UserData?.RoleCode == "1");
public bool CanDelete => (Permision.Delete ?? false) || (UserData?.RoleCode == "1");
```

**Line 37:** Fixed in OnGet method
```csharp
// BEFORE
var canView = (Permision.View ?? false) || UserData.RoleCode == "1";

// AFTER
var canView = (Permision.View ?? false) || (UserData?.RoleCode == "1");
```

### 2. **Create.cshtml.cs** (1 change)
**Line 146:** Fixed CanCreate() method
```csharp
// BEFORE
return (Permision.Add ?? false) || UserData.RoleCode == "1";

// AFTER
return (Permision.Add ?? false) || (UserData?.RoleCode == "1");
```

### 3. **Edit.cshtml.cs** (1 change)
**Line 180:** Fixed CanEdit() method
```csharp
// BEFORE
return (Permision.Edit ?? false) || UserData.RoleCode == "1";

// AFTER
return (Permision.Edit ?? false) || (UserData?.RoleCode == "1");
```

### 4. **Index.cshtml.cs** (4 changes)
**Lines 18-20:** Fixed property definitions
```csharp
// BEFORE
public bool CanCreate => (Permision.Add ?? false) || UserData.RoleCode == "1";
public bool CanEdit => (Permision.Edit ?? false) || UserData.RoleCode == "1";
public bool CanDelete => (Permision.Delete ?? false) || UserData.RoleCode == "1";

// AFTER
public bool CanCreate => (Permision.Add ?? false) || (UserData?.RoleCode == "1");
public bool CanEdit => (Permision.Edit ?? false) || (UserData?.RoleCode == "1");
public bool CanDelete => (Permision.Delete ?? false) || (UserData?.RoleCode == "1");
```

**Line 41:** Fixed in OnGet method
```csharp
// BEFORE
var canView = (Permision.View ?? false) || UserData.RoleCode == "1";

// AFTER
var canView = (Permision.View ?? false) || (UserData?.RoleCode == "1");
```

### 5. **UploadImage.cshtml.cs** (1 change)
**Line 31:** Fixed in OnPostAsync method
```csharp
// BEFORE
var canUpload = (Permision.Add ?? false) || (Permision.Edit ?? false) || UserData.RoleCode == "1";

// AFTER
var canUpload = (Permision.Add ?? false) || (Permision.Edit ?? false) || (UserData?.RoleCode == "1");
```

## Solution Explanation

The fix uses the **null-safe operator (`?.`)** to safely access `RoleCode` property:
- If `UserData` is null, the expression returns null instead of throwing an exception
- The null is then compared with `"1"`, which returns false (safe comparison)
- This prevents the NullReferenceException while maintaining the intended logic

## Testing

✅ **Build Status:** All changes compile successfully without errors  
✅ **Pattern Applied:** Consistent null-safe access across all InternalNews pages  
✅ **Backwards Compatible:** No breaking changes to existing functionality

## Related Pattern

This fix aligns with the codebase pattern where `UserData` is a nullable property initialized in `BaseModel2` constructor but may be null in edge cases before `GetInfoUser()` is called.

---

**Date Fixed:** April 15, 2026  
**Files Modified:** 5  
**Total Changes:** 10
