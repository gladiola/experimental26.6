# Skill: nginx-deployment-checklist

Deploy an ASP.NET Core (Kestrel) app on Ubuntu with nginx as a TLS-terminating reverse proxy.
Established in `gladiola/eurocsv` PR #13 (ForwardedHeaders middleware) and PR #14 (502 runbook).

## Prerequisites

- Ubuntu server with `dotnet` runtime installed
- `nginx` installed and running
- TLS certificate + key available (Let's Encrypt or other)

---

## Step 1 — Publish the application

```bash
dotnet publish <Project>/<Project>.csproj -c Release -o /var/www/<appname>
```

---

## Step 2 — Create a systemd service

Create `/etc/systemd/system/<appname>.service`:

```ini
[Unit]
Description=<AppName> ASP.NET Core application
After=network.target

[Service]
WorkingDirectory=/var/www/<appname>
ExecStart=/usr/bin/dotnet /var/www/<appname>/<appname>.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:<port>
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl daemon-reload
sudo systemctl enable --now <appname>
```

---

## Step 3 — Configure nginx

Create `/etc/nginx/sites-available/<appname>`:

```nginx
server {
    listen 80;
    server_name example.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl;
    server_name example.com;

    ssl_certificate     /etc/ssl/certs/example.com.crt;
    ssl_certificate_key /etc/ssl/private/example.com.key;

    location / {
        proxy_pass         http://localhost:<port>;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   Upgrade           $http_upgrade;
        proxy_set_header   Connection        keep-alive;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Enable:
```bash
sudo ln -s /etc/nginx/sites-available/<appname> /etc/nginx/sites-enabled/
sudo nginx -t && sudo systemctl reload nginx
```

---

## Step 4 — `Program.cs` requirement: `UseForwardedHeaders`

This **must** be added before `UseHttpsRedirection`, or Kestrel will see every request as plain
HTTP and produce infinite redirect loops:

```csharp
using Microsoft.AspNetCore.HttpOverrides;

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
// Then: app.UseHttpsRedirection();
```

---

## Step 5 — 502 Bad Gateway troubleshooting checklist

Work through these in order until the 502 resolves:

1. **Service status** — `sudo systemctl status <appname>` — must show `active (running)`
2. **Startup logs** — `sudo journalctl -u <appname> -n 200 --no-pager` — look for unhandled exceptions
3. **`ExecStart` path** — confirm the `.dll` path in the service file matches the actual publish output; for self-contained deploys use the binary, not `dotnet <app>.dll`
4. **Port parity** — `ASPNETCORE_URLS=http://localhost:<port>` in the service file must match `proxy_pass http://localhost:<port>` in nginx
5. **Upstream probe** — `curl -v http://localhost:<port>/` directly on the server; a response means Kestrel is up and the problem is in nginx
6. **nginx config test** — `sudo nginx -t` — must report `syntax is ok`
7. **Sites-enabled symlink** — confirm the symlink exists in `/etc/nginx/sites-enabled/`
8. **Conflicting `server_name`** — check for another nginx block claiming the same domain
9. **nginx error log** — `sudo tail -50 /var/log/nginx/error.log`
10. **File permissions** — the `User=www-data` service account must be able to read publish dir and write to the temp/log directories
11. **Reload cycle** — `sudo systemctl restart <appname> && sudo systemctl reload nginx` then retest

## Source

`gladiola/eurocsv` PR #13 (`copilot/fix-502-bad-gateway`) and PR #14 (`copilot/check-502-bad-gateway`).
