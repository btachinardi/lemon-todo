import { StoryHeroSection } from '@/domains/landing/components/sections/StoryHeroSection';
import { StoryVisionSection } from '@/domains/landing/components/sections/StoryVisionSection';
import { StoryJourneySection } from '@/domains/landing/components/sections/StoryJourneySection';
import { StoryArchSection } from '@/domains/landing/components/sections/StoryArchSection';
import { StoryDomainsSection } from '@/domains/landing/components/sections/StoryDomainsSection';
import { StoryTechSection } from '@/domains/landing/components/sections/StoryTechSection';
import { StoryNumbersSection } from '@/domains/landing/components/sections/StoryNumbersSection';

/** Development story page — showcases the journey of building LemonDo. */
export function StoryPage() {
  return (
    <>
      <StoryHeroSection />
      <StoryVisionSection />
      <StoryJourneySection />
      <StoryArchSection />
      <StoryDomainsSection />
      <StoryTechSection />
      <StoryNumbersSection />
    </>
  );
}
