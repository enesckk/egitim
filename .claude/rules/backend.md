# Backend Kuralları

## API Tasarımı

- RESTful veya alternatif (GraphQL, gRPC) seçimi mimari aşamasında belirlenecek.
- Endpoint'ler resource-oriented olsun.
- HTTP method'ları doğru kullanılsın (GET=read, POST=create, PUT=update, DELETE=remove).
- Response format tutarlı ve belgeli olsun.
- Error response'ları standart formatta, machine-readable olsun.

## Authentication & Authorization

- Auth kontrolü her endpoint'te middleware/pipeline düzeyinde zorunlu.
- Role-based veya attribute-based yetkilendirme mimari aşamasında belirlenecek.
- Session/token yönetimi backend sorumluluğundadır; frontend sadece token'ı taşır.
- Refresh token stratejisi baştan tasarlanacak.

## İş Mantığı

- İş kuralları servis/use-case katmanında yaşar, controller'da değil.
- Controller sadece HTTP request/response mapping yapsın.
- Transaction boundary'ler açıkça tanımlansın.
- Side effect'ler (email, notification) iş mantığı akışından ayrıştırılsın.

## Input Validation

- Her endpoint'e gelen input validate edilsin.
- Validation kuralları tek yerde tanımlansın, tekrar etmesin.
- Validation hataları kullanıcıya açık ve actionable mesajlarla dönsün.
- Server-side validation zorunludur; client-side yalnızca UX iyileştirmesidir.

## Error Handling

- Beklenen hatalar (validation, not found, conflict) ve beklenmeyen hatalar ayrı ele alınsın.
- Stack trace veya internal detay client'a sızmasın.
- Hatalar loglansın; correlation ID ile izlenebilir olsun.

## Performance

- N+1 query tehlikesine karşı dikkatli ol.
- Pagination her liste endpoint'inde varsayılan olsun.
- Rate limiting stratejisi mimari aşamasında belirlenecek.
