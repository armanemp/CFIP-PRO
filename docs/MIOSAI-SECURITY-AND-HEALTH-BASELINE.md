# MIOSAIAI security and health baseline

The API now has a default trusted-host boundary, request correlation, baseline security response headers and an explicit HSTS production switch. CORS remains configuration-driven.

Health is split from readiness: health reports process-level service state, while readiness exposes component lifecycle state and must not claim provider connectivity until an adapter has actually established it.

Future hardening layers remain:
- authenticated control-plane mutation;
- rate limiting at the edge;
- SSRF-safe research acquisition;
- dependency/SBOM and advisory scanning;
- signed build/artifact verification;
- secret rotation and external secret management;
- production TLS termination and HSTS enablement;
- provider credential isolation.
