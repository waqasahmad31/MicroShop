# Phase 11 — Asynchronous order workflow

Status: NOT STARTED

## Goal and planned deliverables
OrderCreated/StockReserved/StockReservationFailed/OrderConfirmed/OrderRejected events; pending workflow; atomic reservations and explicit cancellation/release transitions.

## Definition of Done
Order confirms or rejects through events; concurrent orders cannot oversell; repeated OrderId cannot double-reserve/release; cancelled order cannot reconfirm; DB/publish gap documented for Phase 13.

## Implementation record
Not started. This file records planned scope, not completed functionality.

At completion record what/why/how/concept, projects/files changed, important classes/interfaces,
request/data flow, database changes/migrations, APIs, tests, exact verified commands/results,
problems, decisions and remaining work. Update the mandatory handoff files before marking complete.


