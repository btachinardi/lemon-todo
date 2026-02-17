import { RoadmapHeroSection } from '@/domains/landing/components/sections/RoadmapHeroSection';
import { RoadmapHorizonSection } from '@/domains/landing/components/sections/RoadmapHorizonSection';
import { RoadmapCtaSection } from '@/domains/landing/components/sections/RoadmapCtaSection';
import {
  SparklesIcon,
  PlugIcon,
  UsersIcon,
  NetworkIcon,
  TerminalIcon,
  SmartphoneIcon,
  TrendingUpIcon,
  Wand2Icon,
  ShieldCheckIcon,
  MessageSquareIcon,
  ServerIcon,
  LinkIcon,
  TagIcon,
  CalendarIcon,
  BellRingIcon,
  MailIcon,
  ZapIcon,
  RadioIcon,
  LayoutGridIcon,
  MessageCircleIcon,
  ActivityIcon,
  GitBranchIcon,
  ListChecksIcon,
  RepeatIcon,
  SlidersHorizontalIcon,
  BarChart3Icon,
  GlobeIcon,
  CodeIcon,
  GitPullRequestIcon,
  MonitorIcon,
  KeyIcon,
  CreditCardIcon,
  LayoutDashboardIcon,
  LineChartIcon,
  RefreshCwIcon,
  CommandIcon,
  Undo2Icon,
  LayersIcon,
  CloudIcon,
  RocketIcon,
  FlagIcon,
  HeartPulseIcon,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';

export interface TierConfig {
  icon: LucideIcon;
  featureIcons: LucideIcon[];
}

const tierConfig: TierConfig[] = [
  { icon: SparklesIcon, featureIcons: [MessageSquareIcon, ServerIcon, LinkIcon, TagIcon] },
  { icon: PlugIcon, featureIcons: [CalendarIcon, BellRingIcon, MailIcon, ZapIcon] },
  { icon: UsersIcon, featureIcons: [RadioIcon, LayoutGridIcon, MessageCircleIcon, ActivityIcon] },
  { icon: NetworkIcon, featureIcons: [GitBranchIcon, ListChecksIcon, RepeatIcon, SlidersHorizontalIcon] },
  { icon: TerminalIcon, featureIcons: [BarChart3Icon, GlobeIcon, CodeIcon, GitPullRequestIcon] },
  { icon: SmartphoneIcon, featureIcons: [MonitorIcon, SmartphoneIcon, KeyIcon, ShieldCheckIcon] },
  { icon: TrendingUpIcon, featureIcons: [CreditCardIcon, LayoutDashboardIcon, LineChartIcon, RefreshCwIcon] },
  { icon: Wand2Icon, featureIcons: [CommandIcon, Undo2Icon, LayersIcon, Wand2Icon] },
  { icon: ShieldCheckIcon, featureIcons: [CloudIcon, RocketIcon, FlagIcon, HeartPulseIcon] },
];

/** Roadmap page showcasing the 9 future capability tiers. */
export function RoadmapPage() {
  return (
    <>
      <RoadmapHeroSection />
      <RoadmapHorizonSection
        horizonKey="near"
        tiers={tierConfig.slice(0, 2)}
        tierStartIndex={0}
      />
      <RoadmapHorizonSection
        horizonKey="mid"
        tiers={tierConfig.slice(2, 5)}
        tierStartIndex={2}
        className="bg-muted/30"
      />
      <RoadmapHorizonSection
        horizonKey="far"
        tiers={tierConfig.slice(5, 9)}
        tierStartIndex={5}
      />
      <RoadmapCtaSection />
    </>
  );
}
