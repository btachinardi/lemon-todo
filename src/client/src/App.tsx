import { RouterProvider } from 'react-router';
import { ErrorBoundaryProvider } from './app/providers/ErrorBoundaryProvider';
import { QueryProvider } from './app/providers/QueryProvider';
import { router } from './app/routes/router';
import { OfflineBanner } from './ui/feedback/OfflineBanner';

/** Root component that wires up error handling, query caching, and client-side routing. */
function App() {
  return (
    <ErrorBoundaryProvider>
      <OfflineBanner />
      <QueryProvider>
        <RouterProvider router={router} />
      </QueryProvider>
    </ErrorBoundaryProvider>
  );
}

export default App;
