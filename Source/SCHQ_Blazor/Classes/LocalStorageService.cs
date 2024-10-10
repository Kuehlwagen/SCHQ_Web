using Microsoft.JSInterop;

namespace SCHQ_Blazor.Classes;

public static class LocalStorageService {

  public static async Task AddValue(IJSRuntime jsRuntime, string key, string value) {
    await jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
  }

  public static async Task RemoveValue(IJSRuntime jsRuntime, string key) {
    await jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
  }

  public static async Task<string?> GetValue(IJSRuntime jsRuntime, string key, bool encrypted = false) {
    string value = await jsRuntime.InvokeAsync<string>("localStorage.getItem", key) ?? string.Empty;
    if ( encrypted && !string.IsNullOrWhiteSpace(value)) {
      value = Encryption.DecryptText(value, key);
    }
    return value;
  }

  public static async Task SetValue(IJSRuntime jsRuntime, string key, string? value = null, bool encrypt = false) {
    if (string.IsNullOrWhiteSpace(value)) {
      await RemoveValue(jsRuntime, key);
    } else {
      if (encrypt) {
        value = Encryption.EncryptText(value, key);
      }
      await AddValue(jsRuntime, key, value);
    }
  }
}
