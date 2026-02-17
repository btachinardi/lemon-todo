import { StoryHeroSection } from '@/domains/landing/components/sections/StoryHeroSection';
import { StoryRationaleSection } from '@/domains/landing/components/sections/StoryRationaleSection';
import { StoryVisionSection } from '@/domains/landing/components/sections/StoryVisionSection';
import { StoryJourneySection } from '@/domains/landing/components/sections/StoryJourneySection';
import { StoryProblemSolvingSection } from '@/domains/landing/components/sections/StoryProblemSolvingSection';
import { StoryTestingSection } from '@/domains/landing/components/sections/StoryTestingSection';
import { StoryHighlightsSection } from '@/domains/landing/components/sections/StoryHighlightsSection';
import { StoryArchSection } from '@/domains/landing/components/sections/StoryArchSection';
import { StoryDomainsSection } from '@/domains/landing/components/sections/StoryDomainsSection';
import { StoryTechSection } from '@/domains/landing/components/sections/StoryTechSection';
import { StoryNumbersSection } from '@/domains/landing/components/sections/StoryNumbersSection';

/** Methodology page — showcases the engineering decisions and journey of building LemonDo. */
export function StoryPage() {
  return (
    <>
      <StoryHeroSection />
      <StoryRationaleSection />
      <StoryVisionSection />
      <StoryJourneySection />
      <StoryProblemSolvingSection />
      <StoryTestingSection />
      <StoryHighlightsSection />
      <StoryArchSection />
      <StoryDomainsSection />
      <StoryTechSection />
      <StoryNumbersSection />
    </>
  );
}
