# 📝 ToDoWebApp - Görev & Ekip Yönetim Paneli

Modern, güvenli ve performans odaklı bir **Görev ve Çalışma Odaları (WorkGroup)** yönetim web uygulaması. ASP.NET Core, Entity Framework Core, Redis önbellekleme, MSSQL ve Docker mimarisi üzerine inşa edilmiştir.

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![MSSQL](https://img.shields.io/badge/Database-SQL_Server-CC292B?logo=microsoftsqlserver)
![Redis](https://img.shields.io/badge/Cache-Redis-DC382D?logo=redis)
![Docker](https://img.shields.io/badge/Container-Docker-2496ED?logo=docker)
![Bootstrap](https://img.shields.io/badge/UI-Bootstrap_5-7952B3?logo=bootstrap)

---

## 🚀 Temel Özellikler

- **Görev Yönetimi:**
  - Görev oluşturma, güncelleme, tamamlama ve silme.
  - Sürükle-bırak (Drag & Drop - SortableJS) ile kategoriler arası dinamik görev taşıma.
  - Öncelik seviyeleri (Düşük, Orta, Yüksek) ve Bitiş Tarihi (Due Date) takibi.
  - Görev tamamlandığında etkileşimli Confetti animasyonu.

- **Çalışma Odaları (WorkGroups / Takım Alanı):**
  - Özel veya ortak çalışma odaları kurma.
  - Rol bazlı yetkilendirme: **Kurucu (Owner)**, **Düzenleyici (Admin)** ve **İzleyici (Viewer)**.
  - Arkadaşları odaya davet etme ve oda yetkilerini yönetme.

- **Sosyal & İletişim:**
  - Kullanıcı arama ve arkadaşlık isteği gönderme / onaylama / reddetme.
  - Görev kartları altında anlık yorumlaşma alanı.

- **Kişiselleştirme & Kullanıcı Deneyimi:**
  - Koyu (Dark) ve Açık (Light) tema desteği.
  - Hazır Dicebear avatarlarından seçim veya yerel profil resmi yükleme.
  - Gerçek zamanlı performans ve istatistik özet kartları.

- **Güvenlik (Production-Ready):**
  - XSS (Cross-Site Scripting) koruması (HTML sanitization).
  - CSRF (Cross-Site Request Forgery) token doğrulaması.
  - Güvenli dosya yükleme ve Path Traversal engelleme.
  - Kaba kuvvet (Brute-force) koruması için Rate Limiting.
  - Güvenli Cookie bayrakları (HttpOnly, SameSite).
  - Kriptografik rastgele tokenlerle şifre sıfırlama (Resend API).

- **Performans:**
  - Redis Dağıtık Önbellek (Distributed Caching) ve otomatik önbellek geçersiz kılma (cache invalidation).
  - Veritabanı ve Redis kesintilerine karşı dayanıklı hata yakalama (Fail-safe).

---

## 🛠️ Teknolojiler

- **Backend:** C#, ASP.NET Core (MVC & Web API), Entity Framework Core
- **Veritabanı:** Microsoft SQL Server 2022
- **Önbellek:** Redis
- **Frontend:** Vanilla JavaScript, HTML5, CSS3, Bootstrap 5, SortableJS, Canvas-Confetti
- **DevOps:** Docker, Docker Compose, Multi-stage Dockerfile

---

## 🐳 Docker ile Hızlı Başlangıç

Projeyi tek bir komutla tüm bağımlılıklarıyla (Uygulama + SQL Server + Redis) ayağa kaldırabilirsiniz:

```bash
docker compose up -d
```

Uygulama hazır olduğunda tarayıcınızdan erişebilirsiniz:
👉 **http://localhost:5000**

---

## 💻 Yerel Geliştirme (Local Setup)

1. **Bağımlılıkları Başlatın (SQL Server & Redis):**
   ```bash
   docker compose up -d db redis_cache
   ```

2. **Veritabanı Migration'larını Uygulayın:**
   ```bash
   dotnet ef database update
   ```

3. **Uygulamayı Çalıştırın:**
   ```bash
   dotnet run
   ```
   Tarayıcınızda açın: `http://localhost:5063`

---

## 📄 Lisans
Bu proje MIT lisansı ile lisanslanmıştır.
