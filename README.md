# TiktokCekilisApp

TikTok video yorumlarini cekip, @etiketli yorumlari katilim olarak sayan
basit bir .NET console uygulamasi. Her yorum 1 katilim sayilir ve istersen
"tek kisi tek hak" veya "coklu hak" modunda kazanan secimi yapilabilir.

## Ozellikler

- Video linkinden yorumlari cekme (Playwright ile)
- @etiket icermeyen yorumlari eleme
- Tek hak / coklu hak secimi
- Kazanan secimi
- Maksimum yorum sayisi limiti

## Gereksinimler

- .NET 8 SDK
- Playwright tarayici kurulumlari

## Kurulum

```powershell
dotnet restore
dotnet build
powershell -ExecutionPolicy Bypass -File ".\bin\Debug\net8.0\playwright.ps1" install
```

## Calistirma

```powershell
dotnet run
```

Uygulama sira ile:

1) Video linkini ister
2) Scroll sayisini ve maksimum yorum sayisini sorar
3) Tek hak / coklu hak secimini sorar
4) Gerekirse tarayicida oturum acman icin bekler
5) Yorumlari cekip kazanan secimi yapar

## Notlar

- TikTok arayuzu ve secicileri zamanla degisebilir. Yorumlar gelmezse
  selector kurallari guncellenmelidir.
- Yorumlar bazen oturum acmadan gorunmeyebilir. Bu durumda tarayicida
  giris yapip konsolda Enter'a basmalisin.
- Cok fazla yorum icin scroll sayisini artir veya maksimum yorum sayisi ver.

## Yasal Uyari

TikTok icerikleri uzerinde otomatik veri cekme, TikTok'un kosullari ve
yerel mevzuata tabi olabilir. Kullanimdan once ilgili kosullari kontrol et.
