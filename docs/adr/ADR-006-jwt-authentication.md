# ADR-006: JWT Access Token + Refresh Token

## Status
Accepted

## Context
Egitim Platformu için authentication mechanism seçimi:

Seçenekler:
1. Session-based (cookie)
2. JWT (JSON Web Token)
3. OAuth2 / OpenID Connect (external provider)
4. Hybrid (JWT + refresh token)

## Decision
**JWT Access Token + Refresh Token** benimsendi.

### Access Token
- **Type:** JWT (JSON Web Token)
- **Lifetime:** 15-30 dakika (kısa ömürlü)
- **Payload:** UserId, InstitutionId, Roles, Expiration
- **Storage:** Client-side memory (localStorage/sessionStorage YASAK — XSS risk)
- **Transmission:** Authorization: Bearer header

### Refresh Token
- **Type:** Opaque token (random string)
- **Lifetime:** 7-30 gün (uzun ömürlü, configurable)
- **Rotation:** Tek kullanımlık — her refresh'te yeni refresh token üretilir
- **Storage:** HttpOnly, Secure, SameSite cookie
- **Revocation:** Token blacklist (database)

### Token Flow
1. Kullanıcı login → access token + refresh token üretilir
2. Access token expiration'a yaklaşınca → refresh endpoint çağrılır
3. Refresh token validate edilir → yeni access token + yeni refresh token
4. Eski refresh token invalid olur
5. Refresh token çalınma şüphesi → tüm token'lar revoke edilir

## Consequences

**Pozitif:**
- Stateless access token — server-side session storage yok
- Kısa ömürlü access token — çalınma riski minimize
- Refresh token rotation — reuse detection
- Scale-out friendly — session state yok

**Negatif:**
- Client-side token yönetimi karmaşık
- XSS saldırısına karşı dikkatli olunmalı
- Refresh token revocation mechanism gerekir

**Mitigation:**
- Access token memory'de tutulur (localStorage değil)
- Refresh token HttpOnly cookie'de
- CORS strict policy
- Rate limiting on refresh endpoint
- Audit log for token events
