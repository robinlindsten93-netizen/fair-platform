import { useState } from "react";

const API_BASE = "http://localhost:5001";

const panelStyle = {
  border: "1px solid #d0d7de",
  borderRadius: 12,
  padding: 20,
  background: "#ffffff",
  boxShadow: "0 1px 3px rgba(0,0,0,0.06)",
  overflow: "auto",
  minHeight: 0
};

const inputStyle = {
  width: "100%",
  padding: "10px 12px",
  borderRadius: 8,
  border: "1px solid #cbd5e1",
  fontSize: 14,
  boxSizing: "border-box"
};

const buttonRowStyle = {
  display: "flex",
  gap: 10,
  flexWrap: "wrap",
  marginTop: 12
};

const preStyle = {
  background: "#0f172a",
  color: "#e2e8f0",
  padding: 12,
  borderRadius: 8,
  overflow: "auto",
  whiteSpace: "pre-wrap",
  wordBreak: "break-word",
  fontSize: 13,
  lineHeight: 1.4,
  maxHeight: 180
};

export default function App() {
  // =========================
  // RIDER AUTH
  // =========================
  const [phone, setPhone] = useState("+46700000001");
  const [code, setCode] = useState("123456");
  const [token, setToken] = useState("");
  const [me, setMe] = useState(null);

  // =========================
  // DRIVER AUTH
  // =========================
  const [driverPhone, setDriverPhone] = useState("+46700000002");
  const [driverCode, setDriverCode] = useState("123456");
  const [driverToken, setDriverToken] = useState("");
  const [driverMe, setDriverMe] = useState(null);

  // =========================
  // SHARED UI
  // =========================
  const [result, setResult] = useState("Inte testat ännu");
  const [loading, setLoading] = useState(false);

  // =========================
  // RIDER TRIP DATA
  // =========================
  const [pickupLat, setPickupLat] = useState("59.3293");
  const [pickupLng, setPickupLng] = useState("18.0686");
  const [dropoffLat, setDropoffLat] = useState("59.3493");
  const [dropoffLng, setDropoffLng] = useState("18.0986");

  const [quoteToken, setQuoteToken] = useState("");
  const [tripId, setTripId] = useState("");

  // =========================
  // DRIVER DATA
  // =========================
  const [driverLat, setDriverLat] = useState("59.3293");
  const [driverLng, setDriverLng] = useState("18.0686");
  const [offers, setOffers] = useState([]);
  const [selectedOfferId, setSelectedOfferId] = useState("");

  // =========================
  // RIDER AUTH FUNCTIONS
  // =========================
  async function otpRequest() {
    setLoading(true);
    setResult("Skickar Rider OTP request...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/auth/otp/request`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phone })
      });

      const text = await res.text();
      setResult(`Rider OTP request status: ${res.status}\n\n${text}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function otpVerify() {
    setLoading(true);
    setResult("Verifierar Rider OTP...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/auth/otp/verify`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phone, code })
      });

      const data = await res.json();
      setToken(data.access_token ?? "");
      setResult(`Rider OTP verify status: ${res.status}\n\nToken sparad.`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function loadMe() {
    setLoading(true);
    setResult("Hämtar Rider /me ...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/me`, {
        headers: {
          Authorization: `Bearer ${token}`
        }
      });

      const text = await res.text();

      try {
        setMe(JSON.parse(text));
      } catch {
        setMe(null);
      }

      setResult(`Rider GET /me status: ${res.status}\n\n${text}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function grantRiderRole() {
    if (!me?.userId) {
      setResult("Rider userId saknas. Kör Load /me först.");
      return;
    }

    setLoading(true);
    setResult("Grantar Rider-roll...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/dev/roles/grant`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          userId: me.userId,
          role: "Rider"
        })
      });

      const text = await res.text();
      setResult(`Grant Rider status: ${res.status}\n\n${text || "OK"}\n\nKör OTP verify igen efter grant.`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  // =========================
  // RIDER TRIP FLOW
  // =========================
  async function createQuote() {
    setLoading(true);
    setResult("Creating quote...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/trips/quotes`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          pickupLat: Number(pickupLat),
          pickupLng: Number(pickupLng),
          dropoffLat: Number(dropoffLat),
          dropoffLng: Number(dropoffLng),
          mode: 0
        })
      });

      const data = await res.json();
      setQuoteToken(data.quoteToken ?? "");
      setResult(`Quote OK\n\n${JSON.stringify(data, null, 2)}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function createTrip() {
    setLoading(true);
    setResult("Creating trip...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/trips`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`
        },
        body: JSON.stringify({
          pickupLat: Number(pickupLat),
          pickupLng: Number(pickupLng),
          dropoffLat: Number(dropoffLat),
          dropoffLng: Number(dropoffLng),
          mode: 0,
          quoteToken
        })
      });

      const text = await res.text();

      let data = null;
      if (text) {
        try {
          data = JSON.parse(text);
        } catch {
          data = null;
        }
      }

      if (!res.ok) {
        setResult(`Create trip failed\nStatus: ${res.status}\n\n${text}`);
        return;
      }

      const createdTripId = data?.tripId ?? data?.id ?? "";
      setTripId(createdTripId);

      setResult(`Trip created\nStatus: ${res.status}\n\n${text || "Tomt svar från backend"}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function requestTrip() {
    setLoading(true);
    setResult("Requesting trip...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/trips/${tripId}/request`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json"
        },
        body: JSON.stringify({
          quoteToken
        })
      });

      const text = await res.text();

      if (!res.ok) {
        setResult(`Request trip failed\nStatus: ${res.status}\n\n${text}`);
        return;
      }

      setResult(`Trip requested\nStatus: ${res.status}\n\n${text || "Tomt svar från backend"}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  // =========================
  // DRIVER AUTH FUNCTIONS
  // =========================
  async function driverOtpRequest() {
    setLoading(true);
    setResult("Skickar Driver OTP request...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/auth/otp/request`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phone: driverPhone })
      });

      const text = await res.text();
      setResult(`Driver OTP request status: ${res.status}\n\n${text}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function driverOtpVerify() {
    setLoading(true);
    setResult("Verifierar Driver OTP...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/auth/otp/verify`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ phone: driverPhone, code: driverCode })
      });

      const data = await res.json();
      const newToken = data.access_token ?? "";
      setDriverToken(newToken);
      setResult(`Driver OTP verify status: ${res.status}\n\nDriver-token sparad.\n\nToken prefix: ${newToken.slice(0, 40)}...`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function loadDriverMe() {
    setLoading(true);
    setResult("Hämtar Driver /me ...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/me`, {
        headers: {
          Authorization: `Bearer ${driverToken}`
        }
      });

      const text = await res.text();

     try {
      const parsed = JSON.parse(text);
      setDriverMe(parsed);
      setResult(`Driver GET /me status: ${res.status}\n\n${JSON.stringify(parsed, null, 2)}`);
    } catch {
      setDriverMe(null);
      setResult(`Driver GET /me status: ${res.status}\n\n${text}`);
 }
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function grantDriverRole() {
    if (!driverMe?.userId) {
      setResult("Driver userId saknas. Kör Driver Load /me först.");
      return;
    }

    setLoading(true);
    setResult("Grantar Driver-roll...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/dev/roles/grant`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${driverToken}`
        },
        body: JSON.stringify({
          userId: driverMe.userId,
          role: "Driver"
        })
      });

      const text = await res.text();
      setResult(`Grant Driver status: ${res.status}\n\n${text || "OK"}\n\nKör Driver OTP verify igen efter grant.`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  // =========================
  // DRIVER ACTIONS
  // =========================
  async function setDriverOnline() {
    setLoading(true);
    setResult("Sätter driver online...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/driver/me/availability`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${driverToken}`
        },
        body: JSON.stringify({
          isOnline: true
        })
      });

      const text = await res.text();
      setResult(`Driver online status: ${res.status}\n\n${text || "OK"}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function sendDriverLocation() {
    setLoading(true);
    setResult("Skickar driver location...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/driver/location`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${driverToken}`
        },
        body: JSON.stringify({
          lat: Number(driverLat),
          lng: Number(driverLng)
        })
      });

      const text = await res.text();
      setResult(`Driver location status: ${res.status}\n\n${text || "OK"}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

  async function getOffers() {
    setLoading(true);
    setResult("Hämtar driver offers...");

    try {
      const res = await fetch(`${API_BASE}/api/v1/dispatch/offers`, {
        headers: {
          Authorization: `Bearer ${driverToken}`
        }
      });

      const text = await res.text();

      let data = [];
      try {
        data = JSON.parse(text);
      } catch {
        data = [];
      }

      setOffers(Array.isArray(data) ? data : []);

      if (Array.isArray(data) && data.length > 0) {
        setSelectedOfferId(data[0].offerId ?? data[0].id ?? "");
      }

      setResult(`Get offers status: ${res.status}\n\n${text}`);
    } catch (err) {
      setResult(`ERROR: ${err.message}`);
    } finally {
      setLoading(false);
    }
  }

     async function acceptOffer() {
     setLoading(true);
     setResult("Accepterar offer...");

   try {
     const res = await fetch(`${API_BASE}/api/v1/dispatch/offers/accept`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${driverToken}`
      },
      body: JSON.stringify({
        offerId: selectedOfferId
      })
    });

    const text = await res.text();
    setResult(`Accept offer status: ${res.status}\n\n${text || "OK"}`);
  } catch (err) {
    setResult(`ERROR: ${err.message}`);
  } finally {
    setLoading(false);
  }
}

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "#f8fafc",
        color: "#0f172a",
        padding: 24,
        boxSizing: "border-box"
      }}
    >
      <div
        style={{
          maxWidth: 1400,
          margin: "0 auto",
          display: "grid",
          gridTemplateRows: "auto 1fr auto",
          gap: 20,
          minHeight: "calc(100vh - 48px)"
        }}
      >
        <header>
          <h1 style={{ margin: 0, fontSize: 40 }}>Fair Frontend 🚀</h1>
          <p style={{ marginTop: 8, color: "#475569" }}>
            Rider + Driver testpanel för backend och dispatchflöden.
          </p>
        </header>

        <main
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 1fr",
            gap: 20,
            minHeight: 0
          }}
        >
          {/* RIDER */}
          <section style={panelStyle}>
            <h2 style={{ marginTop: 0 }}>Rider</h2>

            <div style={{ display: "grid", gap: 10 }}>
              <input
                value={phone}
                onChange={(e) => setPhone(e.target.value)}
                placeholder="Rider Telefon"
                style={inputStyle}
              />
              <input
                value={code}
                onChange={(e) => setCode(e.target.value)}
                placeholder="Rider OTP"
                style={inputStyle}
              />
            </div>

            <div style={buttonRowStyle}>
              <button onClick={otpRequest} disabled={loading}>OTP request</button>
              <button onClick={otpVerify} disabled={loading}>OTP verify</button>
              <button onClick={loadMe} disabled={!token || loading}>Load /me</button>
              <button onClick={grantRiderRole} disabled={!me?.userId || loading}>Grant Rider</button>
            </div>

            <h3 style={{ marginTop: 24 }}>Trip</h3>

            <div style={{ display: "grid", gap: 10 }}>
              <input
                value={pickupLat}
                onChange={(e) => setPickupLat(e.target.value)}
                placeholder="Pickup Lat"
                style={inputStyle}
              />
              <input
                value={pickupLng}
                onChange={(e) => setPickupLng(e.target.value)}
                placeholder="Pickup Lng"
                style={inputStyle}
              />
              <input
                value={dropoffLat}
                onChange={(e) => setDropoffLat(e.target.value)}
                placeholder="Dropoff Lat"
                style={inputStyle}
              />
              <input
                value={dropoffLng}
                onChange={(e) => setDropoffLng(e.target.value)}
                placeholder="Dropoff Lng"
                style={inputStyle}
              />
            </div>

            <div style={buttonRowStyle}>
              <button onClick={createQuote} disabled={!token || loading}>Create Quote</button>
              <button onClick={createTrip} disabled={!quoteToken || loading}>Create Trip</button>
              <button onClick={requestTrip} disabled={!tripId || loading}>Request Trip</button>
            </div>

            <h4 style={{ marginTop: 24 }}>Rider /me</h4>
            <pre style={preStyle}>
              {me ? JSON.stringify(me, null, 2) : "Ingen rider /me-data ännu"}
            </pre>

            <h4>QuoteToken</h4>
            <pre style={preStyle}>{quoteToken || "Ingen quote ännu"}</pre>

            <h4>TripId</h4>
            <pre style={preStyle}>{tripId || "Ingen trip ännu"}</pre>
          </section>

          {/* DRIVER */}
          <section style={panelStyle}>
            <h2 style={{ marginTop: 0 }}>Driver</h2>

            <div style={{ display: "grid", gap: 10 }}>
              <input
                value={driverPhone}
                onChange={(e) => setDriverPhone(e.target.value)}
                placeholder="Driver Telefon"
                style={inputStyle}
              />
              <input
                value={driverCode}
                onChange={(e) => setDriverCode(e.target.value)}
                placeholder="Driver OTP"
                style={inputStyle}
              />
            </div>

            <div style={buttonRowStyle}>
              <button onClick={driverOtpRequest} disabled={loading}>OTP request</button>
              <button onClick={driverOtpVerify} disabled={loading}>OTP verify</button>
              <button onClick={loadDriverMe} disabled={!driverToken || loading}>Load /me</button>
              <button onClick={grantDriverRole} disabled={!driverMe?.userId || loading}>Grant Driver</button>
            </div>

            <h3 style={{ marginTop: 24 }}>Driver actions</h3>

            <div style={{ display: "grid", gap: 10 }}>
              <input
                value={driverLat}
                onChange={(e) => setDriverLat(e.target.value)}
                placeholder="Driver Lat"
                style={inputStyle}
              />
              <input
                value={driverLng}
                onChange={(e) => setDriverLng(e.target.value)}
                placeholder="Driver Lng"
                style={inputStyle}
              />
            </div>

            <div style={buttonRowStyle}>
              <button onClick={setDriverOnline} disabled={!driverToken || loading}>Set Online</button>
              <button onClick={sendDriverLocation} disabled={!driverToken || loading}>Send Location</button>
              <button onClick={getOffers} disabled={!driverToken || loading}>Get Offers</button>
            </div>

            <h4 style={{ marginTop: 24 }}>Offers</h4>
            <pre style={preStyle}>
              {offers.length > 0 ? JSON.stringify(offers, null, 2) : "Inga offers ännu"}
            </pre>

            <input
              value={selectedOfferId}
              onChange={(e) => setSelectedOfferId(e.target.value)}
              placeholder="OfferId"
              style={inputStyle}
            />

            <div style={buttonRowStyle}>
              <button onClick={acceptOffer} disabled={!selectedOfferId || !driverToken || loading}>
                Accept Offer
              </button>
            </div>

            <h4 style={{ marginTop: 24 }}>Driver /me</h4>
            <pre style={preStyle}>
              {driverMe ? JSON.stringify(driverMe, null, 2) : "Ingen driver /me-data ännu"}
            </pre>
          </section>
        </main>

        <footer>
          <h2 style={{ marginBottom: 10 }}>Result</h2>
          <pre
            style={{
              ...preStyle,
              maxHeight: 220,
              margin: 0
            }}
          >
            {result}
          </pre>
        </footer>
      </div>
    </div>
  );
}