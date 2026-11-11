import { useState, type FormEvent } from "react";
import { apiClient } from "../api/client";
import { act, useFetch } from "../api/useFetch";
import type { Subscription, User } from "../types";

const now = new Date();

export function SubscriptionsPage() {
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth() + 1);
  const [amount, setAmount] = useState(1500);
  const { data, error, reload } = useFetch<{ subscription: Subscription; driver: User }[]>(`/admin/subscriptions?year=${year}&month=${month}`);

  function generate(e: FormEvent) {
    e.preventDefault();
    act(() => apiClient.post("/admin/subscriptions/generate", { year, month, amount }), reload);
  }

  return (
    <section>
      <h1>Driver Subscriptions</h1>
      <form onSubmit={generate}>
        <label>
          Year <input id="year" type="number" value={year} onChange={(e) => setYear(Number(e.target.value))} />
        </label>
        <label>
          Month <input id="month" type="number" min={1} max={12} value={month} onChange={(e) => setMonth(Number(e.target.value))} />
        </label>
        <label>
          Amount (LKR) <input id="amount" type="number" min={0} value={amount} onChange={(e) => setAmount(Number(e.target.value))} />
        </label>
        <button type="submit">Bill all active drivers for this month</button>
      </form>
      {error && <p role="alert">{error}</p>}
      <table>
        <thead>
          <tr>
            <th>Driver</th>
            <th>Period</th>
            <th>Amount</th>
            <th>Status</th>
            <th>Paid at</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {data?.map(({ subscription: s, driver }) => (
            <tr key={s.id}>
              <td>{driver.fullName}</td>
              <td>
                {s.billingYear}-{String(s.billingMonth).padStart(2, "0")}
              </td>
              <td>LKR {s.amount}</td>
              <td>{s.status}</td>
              <td>{s.paidAt ? new Date(s.paidAt).toLocaleString() : "—"}</td>
              <td>
                {s.status !== "Completed" && (
                  <button onClick={() => act(() => apiClient.post(`/admin/subscriptions/${s.id}/mark-paid`), reload)}>Mark paid</button>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {data?.length === 0 && <p>No subscriptions for this month yet.</p>}
    </section>
  );
}
