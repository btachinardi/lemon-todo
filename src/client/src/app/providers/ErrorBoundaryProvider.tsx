import { Component, type ErrorInfo, type ReactNode } from 'react';
import { captureError } from '@/lib/error-logger';
import { ErrorFallback } from '@/ui/feedback/ErrorFallback';

interface Props {
  children: ReactNode;
}

interface State {
  error: Error | null;
}

/**
 * App-level React Error Boundary. Catches unhandled rendering errors,
 * logs them via the error logger, and displays a recovery UI.
 */
export class ErrorBoundaryProvider extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { error };
  }

  componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
    captureError(error, {
      source: 'ErrorBoundary',
      metadata: { componentStack: errorInfo.componentStack },
    });
  }

  private handleReset = () => {
    this.setState({ error: null });
  };

  render() {
    if (this.state.error) {
      return <ErrorFallback error={this.state.error} onReset={this.handleReset} />;
    }
    return this.props.children;
  }
}
