# Taʻiala mo le Optimization o le Nonce Generation

## Faafitauli i le taimi nei

Afai e gaosia nonce mo **talosaga uma HTTP**, e tupu:

- galuega crypto e lē manaʻomia,
- valaau tele i auaunaga lilo,
- faaitiitia le performance,
- siitia tau.

---

## Fofo fautuaina

Gaoioi nonce fou **naʻo tali HTML** e manaʻomia CSP nonce.

---

## Fuafuaga Faia

### Filifiliga 1: Path filtering (sili ona faigofie)

Aloese mo:

- `/css`, `/js`, `/images`, `/lib`, `/api`,
- static files ma extensions masani.

Gaoioi nonce mo page requests naʻo.

### Filifiliga 2: Response pipeline (fautuaina)

Faaaoga `Response.OnStarting` ma siaki `ContentType` e iai `text/html` aʻo leʻi tuʻu headers.

### Filifiliga 3: Lazy generation

Gaoioi nonce pe a fausia CSP header, ma faʻaoga cache puʻupuʻu pe a talafeagai.

---

## Aafiaga o le performance

Faataitaiga:

- muamua: nonce 1000/1000 requests,
- mulimuli: nonce ~100/1000 requests (pe a 10% page requests),
- faaitiitia tele valaau ma tau.

---

## Fautuaga mo WebAppExperimental266

- Taofi nonce i `HttpContext.Items` i request ta‘itasi.
- Aua le faalagolago i fallback nonce tumau.
- Aua le gaosia nonce mo static content.
- Faamaonia o CSP nonce e fetaui ma script tags i response.

---

## Suʻega fautuaina

1. Talosaga i `/` ma `/Privacy` → e tatau ona gaosia nonce.
2. Talosaga i `/css/site.css` → e tatau ona skip nonce generation.
3. Siaki logs ma metrics mo le paʻu o nonce-generation count.

---

## Lisi Migration

- [ ] Backup middleware tuai
- [ ] Fausia optimized middleware
- [ ] Faafou `Program.cs`
- [ ] Suʻega static/page requests
- [ ] Mataʻitu metrics ma logs

---

## Iʻuga faʻamoemoe

- Faaitiitia nonce generation calls,
- Faaleleia response time mo static content,
- Taofia pea le saogalemu CSP mo HTML pages.
