# Güvenlik Kuralları

## Kimlik Doğrulama

- Şifreler hash'lenerek saklanmalı (bcrypt, argon2). Düz metin asla.
- Session/token yönetimi backend sorumluluğunda.
- Brute force koruması (rate limiting, account lockout) uygulanmalı.
- Multi-factor authentication ihtiyaç durumunda eklenmeli.

## Yetkilendirme

- Her endpoint erişim kontrolü ile korunmalı.
- yetkilendirme kontrolü backend'de zorunlu; frontend sadece UX'tir.
- Principle of least privilege uygulanmalı.
- IDOR (Insecure Direct Object Reference) kontrolleri her resource erişiminde yapılmalı.

## Input Güvenliği

- Tüm kullanıcı girdileri validate edilmeli.
- XSS koruması: output encoding zorunlu.
- CSRF koruması: token mekanizması uygulanmalı.
- SQL injection: parameterized query zorunlu.
- File upload: type validation, boyut limiti, güvenli depolama.

## Hassas Veri

- Secret, API key, password, connection string kaynak koda yazılmamalı.
- Environment variables veya secret management çözümü kullanılmalı.
- Log'lara hassas veri (şifre, token, kredi kartı) yazılmamalı.
- Hata mesajlarında internal detay (stack trace, SQL) client'a sızmasın.

## Bağımlılık Güvenliği

- Third-party bağımlılıklar düzenli güncellenmeli.
- Bilinen güvenlik açığı olan paketler kullanılmamalı.
- Bağımlılık ekleme kararı yargılı alınmalı.

## Transport

- HTTPS zorunlu.
- HSTS header'ı production'da aktif olmalı.
- Güvenli cookie flag'ları (Secure, HttpOnly, SameSite) kullanılmalı.

## OWASP

- OWASP Top 10 kontrol listesi her önemli değişiklikte gözden geçirilmeli.
- Kritik değişikliklerde `security-review` veya `security-guidance` skill kullanılmalı.
