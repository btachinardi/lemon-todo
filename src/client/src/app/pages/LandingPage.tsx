import { HeroSection } from '@/domains/landing/components/sections/HeroSection';
import { FeaturesSection } from '@/domains/landing/components/sections/FeaturesSection';
import { OpenSourceSection } from '@/domains/landing/components/sections/OpenSourceSection';
import { SecuritySection } from '@/domains/landing/components/sections/SecuritySection';
import { HowItWorksSection } from '@/domains/landing/components/sections/HowItWorksSection';
import { CtaSection } from '@/domains/landing/components/sections/CtaSection';

/** Landing page composing all sections. Accessible at `/` for unauthenticated users. */
export function LandingPage() {
  return (
    <>
      <HeroSection />
      <div id="features">
        <FeaturesSection />
      </div>
      <OpenSourceSection />
      <div id="security">
        <SecuritySection />
      </div>
      <HowItWorksSection />
      <CtaSection />
    </>
  );
}
