import { RouterProvider } from 'react-router';
import { QueryProvider } from './app/providers/QueryProvider';
import { router } from './app/routes/router';

/** Root component that wires up query caching and client-side routing. */
function App() {
  return (
    <QueryProvider>
      <RouterProvider router={router} />
    </QueryProvider>
  );
}

export default App;
