# DersProgram� (ASP.NET Core + EF Core)

��retmenlerin haftal�k ders programlar�n� y�netebildi�i, admin panelinden fak�lte/b�l�m/ders tan�mlar�n�n yap�ld��� bir web uygulamas�.

## Ekran G�r�nt�leri
![screenshot](Screenshot/AdminPanel.png)
![screenshot](Screenshot/DersEkleme.png)
![screenshot](Screenshot/OgretmenDersProgramlari.png)
![screenshot](Screenshot/OgretmenOnay.png)
![screenshot](Screenshot/OgretmenPanel.png)



## ��indekiler
- [�zellikler](#�zellikler)
- [Mimari](#mimari)
- [Kurulum](#kurulum)
- [Veritaban�](#veritaban�)
- [Rol ve Ak��](#rol-ve-ak��)
- [Konfig�rasyon](#konfig�rasyon)
- [Raporlama ve D��a Aktar�m](#raporlama-ve-d��a-aktar�m)
- [Ekran G�r�nt�leri](#ekran-g�r�nt�leri)
- [Katk�da Bulunma](#katk�da-bulunma)
- [Lisans](#lisans)

## �zellikler
- Admin:
  - Fak�lte/B�l�m/Ders/Saat tan�mlar�
  - ��retmen listesi ve ba�vurular�n onaylanmas�
  - ��retmene unvan atama ve katsay� y�netimi (maa� hesab� i�in)
  - ��retmenlerin program�n� g�r�nt�leme/d�zenleme
- ��retmen:
  - Haftal�k program�n� g�rme ve y�netme (onayl� ise)
  - Unvan�na g�re maa� hesaplamas� ve ders y�k� raporu
  - Excel�e tek t�kla d��a aktarma (Program + Y�k + Maa� ayn� dosyada 3 sayfa)

## Mimari
- **ASP.NET Core MVC** + **Identity** (Roller: `Admin`, `Teacher`)
- **Entity Framework Core** (SQL Server)
- UI: Bootstrap 5
- Paketler:
  - ClosedXML (Excel): MIT
  - EF Core / Identity: MIT
  - Bootstrap / jQuery: MIT

## Kurulum
1. `appsettings.json` i�inde `DefaultConnection`�� kendi SQL Server��na g�re g�ncelle.
2. Veritaban�n� olu�tur:
   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef database update
