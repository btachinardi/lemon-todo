import { DevOpsHeroSection } from '@/domains/landing/components/sections/DevOpsHeroSection';
import { DevOpsPipelineSection } from '@/domains/landing/components/sections/DevOpsPipelineSection';
import { DevOpsInfraSection } from '@/domains/landing/components/sections/DevOpsInfraSection';
import { DevOpsModulesSection } from '@/domains/landing/components/sections/DevOpsModulesSection';
import { DevOpsLiveSection } from '@/domains/landing/components/sections/DevOpsLiveSection';
import { DevOpsObservabilitySection } from '@/domains/landing/components/sections/DevOpsObservabilitySection';
import { DevOpsCtaSection } from '@/domains/landing/components/sections/DevOpsCtaSection';

/** DevOps page showcasing CI/CD, Terraform infrastructure, and observability. */
export function DevOpsPage() {
  return (
    <>
      <DevOpsHeroSection />
      <DevOpsPipelineSection />
      <DevOpsInfraSection />
      <DevOpsModulesSection />
      <DevOpsLiveSection />
      <DevOpsObservabilitySection />
      <DevOpsCtaSection />
    </>
  );
}
