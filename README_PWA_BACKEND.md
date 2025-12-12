# RealEstateApi – Frontend (Vercel) CORS Setup

This backend was updated to allow requests from the deployed frontend:

- Allowed Origin (CORS): `https://almustashar-frontend-hznb.vercel.app`

## What was changed
- Added a CORS policy named `AllowVercel`
- Enabled the policy in the middleware pipeline using `app.UseCors("AllowVercel");`

## Notes
- If you change your Vercel domain, update the origin in `Program.cs`.
- If you deploy the API to production, ensure you set the correct connection string and secrets via environment variables.
