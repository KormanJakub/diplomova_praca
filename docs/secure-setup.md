# Bezpečné lokálne spustenie a nasadenie API

Hodnoty citlivých nastavení boli odstránené z aktuálnych konfiguračných súborov. Staršie commity ich však stále môžu obsahovať, preto treba **rotovať** prihlasovací údaj MongoDB, Stripe secret key, SMTP heslo a JWT podpisový kľúč. Samotné vymazanie z aktuálneho stromu ich nezneplatní. Po rotácii preverte prístupy a logy dotknutých služieb.

Pre lokálny vývoj použite .NET User Secrets v `api/nia_api`, napríklad:

```powershell
dotnet user-secrets set "NiaDbSettings:ConnectionString" "<nova-lokalna-testovacia-db>"
dotnet user-secrets set "JwtConfig:Key" "<novy-nahodny-kluc>"
dotnet user-secrets set "Stripe:SecretKey" "<novy-stripe-test-kluc>"
dotnet user-secrets set "Stripe:WebhookSecret" "<stripe-webhook-secret>"
dotnet user-secrets set "Smtp:Username" "<smtp-ucet>"
dotnet user-secrets set "Smtp:Password" "<nove-smtp-heslo>"
```

V nasadení tieto hodnoty dodajte cez tajomstvá hostingu alebo environment variables s `__` namiesto `:` (napr. `JwtConfig__Key`). JWT issuer a audience musia zodpovedať konfigurácii API. CORS povoľuje hodnotu `Hosting:Web-Url`; nastavte skutočnú URL webu.

Stripe webhook nasmerujte na `POST /payment/stripe-webhook`, aktivujte udalosti `checkout.session.completed` a `checkout.session.async_payment_succeeded` a nastavte jeho podpisový secret. Bez toho sa platba overí až pri návrate zákazníka na stránku úspechu. Platby a objednávky sa preto pred nasadením musia otestovať v oddelenom Stripe test prostredí.

Produkčné Angular API URL sa zatiaľ nedá správne nastaviť, pretože backend ešte nemá verejnú adresu. Pred publikovaním webu nastavte produkčný environment na adresu nového API a otestujte celý checkout. Existujúce integračné testy vyžadujú izolovanú MongoDB na `127.0.0.1`; nespúšťajte ich proti produkčnej databáze.
