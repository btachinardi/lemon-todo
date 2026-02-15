import { Component, type ErrorInfo, type ReactNode } from 'react';
import { AlertCircleIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import { captureError } from '@/lib/error-logger';

interface Props {
  children: ReactNode;
}

interface State {
  error: Error | null;
}

/**
 * Per-route error boundary that catches rendering crashes within a page
 * while keeping the dashboard header/shell visible.
 * Shows an inline error UI with recovery options.
 */
export class RouteErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    captureError(error, {
      source: 'RouteErrorBoundary',
      metadata: { componentStack: errorInfo.componentStack },
    });
  }

  private handleReset = () => {
    this.setState({ error: null });
  };

  render() {
    if (this.state.error) {
      return (
        <div className="flex flex-col items-center justify-center gap-4 py-20" data-testid="route-error-boundary">
          <div className="rounded-full bg-destructive/10 p-3">
            <AlertCircleIcon className="size-8 text-destructive" />
          </div>
          <div className="text-center">
            <p className="text-lg font-semibold">Something went wrong</p>
            <p className="mt-1 text-sm text-muted-foreground">
              This page encountered an error. Try again or go back.
            </p>
          </div>
          <div className="flex gap-2">
            <Button variant="default" onClick={this.handleReset}>
              Try again
            </Button>
            <Button variant="outline" onClick={() => (window.location.href = '/')}>
              Go to Board
            </Button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
