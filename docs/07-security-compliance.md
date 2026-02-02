# Step 7 - Security and Compliance

## Authentication

### JWT Tokens
- Short-lived access tokens (15 minutes)
- Refresh tokens (7 days, rotating)
- Revoke all tokens on sign-out
- Token stored in secure storage (Keychain/Keystore)

### Password Security
- BCrypt or Argon2 hashing
- Minimum 8 characters
- Prevent common passwords
- Optional 2FA support

### Session Management
- Single active session per device (optional)
- Force logout on password change
- Audit log for authentication events

## Authorization

### Role-Based Access Control
- **Admin**: Full access, server configuration
- **User**: Own photos and albums only

### Resource Ownership
- Photos are private by default
- Albums can be shared via links
- Validate ownership on every request

### Shared Link Security
- Unique random tokens (64 chars)
- Optional password protection
- Expiry date support
- View count tracking
- Revocable by owner

## Data Protection

### In Transit
- TLS 1.3 for all network calls
- Certificate pinning (optional)
- No HTTP fallback

### At Rest
- Encrypt sensitive data on device
- Use OS keychain/secure storage
- Database encryption (optional)

### Signed URLs
- Time-limited download/upload URLs
- Prevent unauthorized access
- Include request validation

## Permissions

### Mobile
- Request Photos permission at moment of need
- Request Camera permission only when needed
- Explain why permission is needed
- Handle denied permissions gracefully

```csharp
public async Task<bool> CheckAndRequestPhotosPermissionAsync()
{
    var status = await Permissions.CheckStatusAsync<Permissions.Photos>();

    if (status == PermissionStatus.Denied)
    {
        // Show explanation UI
        await ShowPermissionExplanationAsync();
    }

    if (status != PermissionStatus.Granted)
    {
        status = await Permissions.RequestAsync<Permissions.Photos>();
    }

    return status == PermissionStatus.Granted;
}
```

## Performance-Aware Security

### Best Practices
- Use OS keychain/secure storage (avoid custom crypto)
- Reuse HTTP connections to reduce TLS handshake overhead
- Limit sensitive logging to reduce disk I/O
- Cache authentication state appropriately

### Avoid
- Custom encryption implementations
- Storing tokens in plain text
- Logging sensitive data
- Excessive security checks in hot paths

## Privacy

### Data Retention
- Clear retention policy for deleted photos (30 days default)
- Permanent deletion after retention period
- User can request immediate deletion

### User Rights
- Export all data (GDPR Article 20)
- Delete account and all data
- View what data is stored
- Opt-out of analytics (if any)

### Photo Metadata
- Option to strip location data on upload
- Control what metadata is shared
- Inform users about metadata exposure

## Compliance

### GDPR Readiness
- Data processing transparency
- Right to access, rectify, erase
- Data portability
- Consent management
- Privacy by design

### CCPA Readiness
- Do not sell personal information
- Right to know what data is collected
- Right to delete
- Non-discrimination

### Age Restrictions
- Consider age verification if required
- Parental consent for minors (where applicable)

## API Security

### Rate Limiting
- Per-user rate limits
- Per-IP rate limits for unauthenticated endpoints
- Exponential backoff on failures

### Input Validation
- Validate all inputs
- Sanitize file names
- Check file types and sizes
- Prevent path traversal

### File Upload Security
- Validate MIME types
- Check file signatures (magic bytes)
- Store outside web root
- Generate unique file names

## Audit Logging

### Events to Log
- Authentication attempts (success/failure)
- Password changes
- Photo uploads/deletes
- Share link creation
- Account changes

## Deliverables

- Security review checklist
- Privacy policy draft
- Data deletion workflow
- GDPR compliance documentation
