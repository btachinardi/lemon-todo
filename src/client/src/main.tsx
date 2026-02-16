import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import './i18n/config'
import App from './App.tsx'
import { captureError } from './lib/error-logger'
import { initNetworkMonitoring } from './lib/network-status'
import { initPWA } from './lib/pwa'
import { initWebVitals } from './lib/web-vitals'

// Global handlers for errors that escape React's error boundaries
window.addEventListener('unhandledrejection', (event) => {
  captureError(event.reason, { source: 'unhandledrejection' });
});

window.addEventListener('error', (event) => {
  captureError(event.error, { source: 'window.onerror' });
});

initNetworkMonitoring();
initPWA();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)

initWebVitals();
