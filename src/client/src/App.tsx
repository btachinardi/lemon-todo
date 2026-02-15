import { RouterProvider } from 'react-router';
import { AuthHydrationProvider } from './app/providers/AuthHydrationProvider';
import { ErrorBoundaryProvider } from './app/providers/ErrorBoundaryProvider';
import { QueryProvider } from './app/providers/QueryProvider';
import { router } from './app/routes/router';
import { OfflineBanner } from './ui/feedback/OfflineBanner';

/** Root component that wires up error handling, query caching, and client-side routing. */
function App() {
  return (
    <ErrorBoundaryProvider>
      <OfflineBanner />
      <AuthHydrationProvider>
        <QueryProvider>
          <RouterProvider router={router} />
        </QueryProvider>
      </AuthHydrationProvider>
    </ErrorBoundaryProvider>
  );
}

export default App;
