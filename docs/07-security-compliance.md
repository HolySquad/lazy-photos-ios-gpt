# Step 7 - Security and Compliance

Permissions
- Request Photos and Camera permissions at the moment of need.

Authentication
- Short-lived access tokens with refresh.
- Revoke tokens on sign-out.

Data protection
- TLS for all network calls.
- Encrypt sensitive data on device.
- Use signed URLs for uploads and downloads.

Performance-aware security
- Use OS keychain/secure storage; avoid custom crypto.
- Reuse HTTP connections to reduce handshake overhead.
- Limit sensitive logging to reduce disk IO on older devices.

Privacy
- Clear retention policy for deleted photos.
- Allow account deletion and export.

Compliance
- GDPR and CCPA readiness.
- Age restrictions if required by region.

Deliverables
- Security review checklist.
- Privacy policy draft.
- Data deletion workflow.
