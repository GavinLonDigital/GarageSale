# Stripe Checkout sandbox demonstration

GarageSale uses a single Stripe merchant, hosted Checkout, USD, and Stripe.net
52.4.2 (API version `2026-08-26.dahlia`). There is no Stripe Connect, payout,
Stripe.js, or publishable-key integration. Never use real cards or live mode.

## Configuration and Docker

1. Sign in to [Stripe Dashboard](https://dashboard.stripe.com), select or create
   a **sandbox** in the account picker, and open its API keys page. Copy the
   sandbox **secret key**, starting with `sk_test_`. Enable the payment methods
   you want to demonstrate in that same sandbox's Dashboard settings.
   See [Stripe keys](https://docs.stripe.com/keys) and
   [dynamic payment methods](https://docs.stripe.com/payments/payment-methods/dynamic-payment-methods).
2. Add these to your existing local `.env` without replacing its SQL/OpenAI settings:

   ```dotenv
   STRIPE_SECRET_KEY=sk_test_REPLACE_LOCALLY
   STRIPE_WEBHOOK_SECRET=whsec_REPLACE_FROM_LISTENER
   APP_BASE_URL=http://localhost:8080
   ```

   `.env.example` documents all Compose variables. It contains no credentials.
   Both Compose files explicitly interpolate these three Stripe variables into
   the `garagesale` service's `environment`. ASP.NET Core configuration reads
   them and `Program.cs` maps them to `StripeOptions`. Compose's `.env`
   interpolation alone would not inject unmapped values. No dotenv library is
   used; `.env` and `.env.*` are excluded from Git and Docker builds (the safe
   `.env.example` is intentionally tracked). The development source bind mount
   follows the existing setup; the application does not read `.env` from it.
3. `APP_BASE_URL` is the address the **buyer's browser** can reach. The existing
   development Compose setup publishes **HTTP 8080**, so use exactly
   `http://localhost:8080`. Do not use `sqlserver`, `garagesale`, a container IP,
   or `http://+:8080`. The value must be an origin without a path, credentials,
   query, or fragment. HTTP is accepted only for loopback; use HTTPS for an
   external domain. Adaptive Pricing is disabled so amounts remain USD.
4. Recreate the development application after changing any environment values:

   ```sh
   docker compose -f docker-compose.dev.yml config --quiet
   docker compose -f docker-compose.dev.yml up -d --build --force-recreate garagesale
   ```

   This starts SQL Server as a dependency if needed. A plain `docker restart`
   keeps the old environment. Do not run `down -v`; keep your database volume.
   The production Compose file references a prebuilt registry image, so use the
   development command above to run this local implementation. Its existing
   production HTTPS redirection expects an appropriate HTTPS deployment setup.

No certificate is required for the current local HTTP configuration. If you
later configure local HTTPS, you must supply/trust a development certificate
and configure Kestrel or your HTTPS proxy plus its published port yourself;
`dotnet dev-certs https --trust` on the host alone does not configure Docker.
Update both browser and forwarding addresses to match that setup.

### Database migration

The existing app applies EF migrations on startup. The new `AddStripeCheckout`
migration only creates `PaymentAttempts`, `InventoryReservations`, and
`StripeWebhookReceipts` with indexes and foreign keys. It does not delete or
reset existing rows. Existing integer prices represent USD cents, and each
product has quantity one. `Pending`, `Paid`, `Failed`, and `Expired` order
states coexist with the existing `Delivered` state. Products can be `Reserved`.

Before starting against an existing database, back it up and inspect its
`__EFMigrationsHistory`. In particular, the **existing**, earlier
`20260708170158_ResetOrdersAndBackfillSalesStatus` migration deletes orders.
It must already be applied on an established database; do not blindly apply
that old reset to a database containing orders. Resolve an older baseline
safely before startup. This integration does not modify that migration or run
migrations against your existing data during verification.

To review only the new migration before applying it:

```sh
dotnet ef migrations script 20260708170158_ResetOrdersAndBackfillSalesStatus 20261002121216_AddStripeCheckout -o stripe-checkout.sql
```

Review the script, take a backup, then either let the normal application startup
apply it or have your database administrator apply it once to the matching
baseline. Do not roll back payment tables after taking payments.

## Local webhooks (Stripe CLI on the host)

Install the CLI using [Stripe's installation guide](https://docs.stripe.com/cli/install).
For macOS with Homebrew:

```sh
brew install stripe/stripe-cli/stripe
stripe login
stripe login list
stripe switch
```

In the browser login and interactive `switch`, select the **same sandbox** whose
`sk_test_` key the app uses. Use `stripe reauth` if it is not in the authorized
list. Current CLI versions may require your account administrator to enable CLI
access. Older CLI versions can use `stripe login --interactive` to enter the
same sandbox key privately; do not record the key entry. Never pass `--live`.
See [CLI account/sandbox selection](https://docs.stripe.com/cli).

The SDK is pinned to the listener's current `2026-08-26.dahlia` API version.
During the browser end-to-end test, Stripe.net 53.0.0 expected the newer
`endive` API family and rejected actual CLI deliveries with HTTP 400.
Keep the SDK and listener API families aligned; do not disable the SDK's
API-version or signature validation to accept an incompatible delivery.

Keep this host terminal running:

```sh
stripe listen --latest --forward-to http://localhost:8080/api/stripe/webhook --events checkout.session.completed,checkout.session.async_payment_succeeded,checkout.session.async_payment_failed,checkout.session.expired
```

The host CLI reaches the app through Docker's published port **8080**, not
through a Docker service name. Copy the listener's printed `whsec_...` signing
secret into `STRIPE_WEBHOOK_SECRET`, then recreate the app using the command
above. The listener secret differs from a Dashboard endpoint secret. Repeat
the edit/recreation whenever it changes. Use snapshot events compatible with
`2026-08-26.dahlia`; `--latest` selects the latest API version. If Stripe releases
a newer incompatible API family, align the SDK and listener/endpoint version
before updating; incompatible events are rejected, not silently accepted.

The current `APP_BASE_URL` and CLI target share `http://localhost:8080`; only
the webhook address has `/api/stripe/webhook`. If your browser uses an HTTPS
proxy but the host listener reaches a separate published HTTP port, use the
browser-facing HTTPS origin for `APP_BASE_URL` and the host-reachable URL for
`--forward-to`. For an intentionally configured local HTTPS server the command
would be `stripe listen --forward-to https://localhost:<published-port>/api/stripe/webhook`;
trust its certificate, or use `--skip-verify` only for that local demonstration.

## Sandbox demonstrations

Sign in as a GarageSale buyer, add available items, open Checkout, and press
**Pay with Stripe (sandbox)**. The session prefills
`test+location_US@example.com` for a simulated US customer without altering
the buyer's profile. Use:

| Scenario | Card | Other details |
| --- | --- | --- |
| Successful US Visa | `4242 4242 4242 4242` | Any future expiry; CVC `123`; United States; ZIP `10001` |
| Declined payment | `4000 0000 0000 0002` | Any future expiry; CVC `123`; United States; ZIP `10001` |

These are [Stripe test cards](https://docs.stripe.com/testing). A decline usually
leaves the Checkout session open so the buyer can try another test card.
It does not automatically fail the entire order. Returning via the cancel link
also leaves it payable and reserved; returning to checkout resumes that session.

Observe the Checkout Session, PaymentIntent, and events in the same sandbox's
Dashboard/Workbench. Session `client_reference_id` is the GarageSale order ID,
and `payment_attempt_id` metadata identifies the durable attempt. The buyer's
return page shows authorized server state and checks pending orders automatically
every two seconds until they reach a final status. The
existing admin order history shows the order status as well.

To demonstrate webhook confirmation, complete a payment, watch the CLI forward
`checkout.session.completed` with a `200` response, then watch the order page update
to see **Paid** and sold inventory. A fast return may initially show **Pending**.
Browser arrival never changes payment state. For delayed methods, completion
stays **Pending** until `async_payment_succeeded` or `async_payment_failed`.
The background reconciliation can also recover verified paid/expired sessions
once per minute, so stopping the CLI alone is not a reliable way to keep a
paid order pending for a recording.

Simulated US customer location and USD do **not** change the merchant account's
registered country. Method availability depends on merchant country, enabled
capabilities/settings, and customer/device eligibility. Methods are managed in
the Dashboard; there is no card-only list in the code. Choose supported methods
for your merchant account rather than expecting every US method to appear.

## Reservation and recovery policy

- The database reserves each unique product atomically before Stripe creation.
  One active attempt per buyer prevents multiple sessions from repeated clicks;
  the persisted request/idempotency key also handles lost responses.
- Open Checkout sessions expire after Stripe's default **24 hours**. Verified
  expiry releases stock and preserves the cart. Merely reaching that clock time
  locally never releases stock without confirming the session's actual state.
- Completed-but-unpaid sessions retain reservations through delayed settlement,
  even beyond the original session expiry timestamp. Only verified async failure
  releases them; verified payment sells them and clears only purchased cart rows.
  No payment-method restriction is required by this policy. Missing async failure
  delivery can leave a hold until the event is resent or manually reconciled.
- Stripe validation rejection releases stock; ambiguous network failures retain
  it. Retry checkout or wait for the worker. Unknown create responses older than
  **23 hours** require manual review: Stripe may prune idempotency keys after
  24 hours, so the app deliberately does not create a possible duplicate charge.
  Keep the same sandbox key/account while attempts are pending. Use Dashboard
  metadata/API logs to find the original session, record its ID on the attempt
  through a reviewed database update, and resend the real matching event or let
  the worker reconcile. Confirm a definitive terminal Stripe state before
  releasing any stock. There is no automated refund or manual recovery UI.
- An anomalous late paid event after a terminal release is rejected for manual
  reconciliation rather than silently reselling the unique product. Failed
  persistence returns a non-2xx so Stripe can retry. Paid orders never downgrade.
- Admin **Clear Orders** is blocked once Stripe payment history exists. Preserve
  the payment audit records; there are no new email or shipment side effects.

Generic `stripe trigger checkout.session.completed` fixtures don't contain your
stored order/attempt metadata and are ignored. Use actual sandbox Checkout for
the end-to-end demonstration; automated tests use synthetic matching sessions.

Hide `STRIPE_SECRET_KEY`, `STRIPE_WEBHOOK_SECRET`, SQL passwords, OpenAI keys,
CLI credentials/configuration, `.env`, and any real personal data while recording.
Do not display resolved `docker compose config` or container environment dumps.
Use `config --quiet` for validation.

## Credential-free verification

```sh
dotnet build GarageSale.csproj
dotnet test Tests/GarageSale.Tests.csproj
docker compose -f docker-compose.dev.yml config --quiet
docker build -f Dockerfile -t garagesale-stripe-demo .
```

The default tests use disposable SQLite databases and a mocked Stripe boundary.
They cover database amounts, ownership, pending/paid/failure/expiry transitions,
signatures, duplicate/concurrent webhooks, retries, and competing stock claims.
For SQL Server concurrency checks, set `GARAGESALE_TEST_SQLSERVER` to a connection
to a **disposable test server** and run the same tests. Each fixture creates and
deletes only a unique `GarageSaleStripeTests_*` database; the login needs database
creation permission. Never point that optional test setting at a production server.
Tests make no Stripe calls and need no Stripe credentials.
