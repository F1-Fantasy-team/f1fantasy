import { PlusOutlined, TeamOutlined } from "@ant-design/icons";
import { F1Title, F1Button } from "../atoms";
import { EmptyState } from "../molecules";
import { GroupList } from "./GroupList";
import type { Group } from "../types/group";

/** Real F1 photo for dashboard banner (Wikimedia Commons, CC). Mercedes W09. */
const DASHBOARD_BANNER_IMAGE_URL =
  "https://upload.wikimedia.org/wikipedia/commons/3/3f/Lewis_Hamilton%27s_2018_Mercedes_AMG_W09_Hybrid_Formula_1_Car_%2852339096904%29.jpg";

/** Banner: F1 photo as background with How it works / Rules text overlaid. */
function InfoBanner() {
  return (
    <section className="relative min-h-[220px] sm:min-h-[260px] rounded-lg border border-f1-gray overflow-hidden bg-f1-black">
      <img
        src={DASHBOARD_BANNER_IMAGE_URL}
        alt=""
        className="absolute inset-0 h-full w-full object-cover"
        loading="lazy"
      />
      <div className="absolute inset-0 bg-black/60" aria-hidden />
      <div className="relative z-10 space-y-3 p-4 sm:p-5 md:p-6">
        <F1Title level={5} className="!mb-1">How it works</F1Title>
        <p className="text-sm text-f1-silver max-w-2xl">
          Create or join a group with an invite code. Make predictions across seven categories (drivers, constructors, draft, destructors, Mr Saturday, zero pointers, wildcard). Compare your picks with friends and see who scores the most when the season unfolds.
        </p>
        <F1Title level={5} className="!mb-1 !mt-2">Rules</F1Title>
        <p className="text-sm text-f1-silver max-w-2xl">
          Points are awarded per category based on real-world results. Check each category for scoring details. The group standings show combined scores—highest total wins.
        </p>
        <p className="text-sm text-f1-silver max-w-2xl">
          <strong className="text-f1-silver">Locking predictions.</strong> When you’re happy with your picks, lock your predictions. Once locked, you can’t edit them unless you unlock again before the season starts.
        </p>
        <p className="text-sm text-f1-silver max-w-2xl">
          <strong className="text-f1-silver">Season start.</strong> When the season starts (e.g. first race), all predictions are automatically locked and can no longer be changed.
        </p>
      </div>
    </section>
  );
}

type DashboardContentProps = {
  groups: Group[];
  onCreateGroup: () => void;
  onJoinGroup: () => void;
};

export function DashboardContent({ groups, onCreateGroup, onJoinGroup }: DashboardContentProps) {
  return (
    <div className="space-y-6 sm:space-y-8">
      <InfoBanner />

      <div className="flex flex-col gap-4 sm:flex-row sm:flex-wrap sm:items-center sm:justify-between">
        <F1Title level={2} className="!text-xl sm:!text-2xl">Your groups</F1Title>
        <div className="flex flex-col gap-2 sm:flex-row sm:gap-2">
          <F1Button size="large" icon={<TeamOutlined />} onClick={onJoinGroup} className="min-h-[44px] w-full sm:w-auto">
            Join group
          </F1Button>
          <F1Button type="primary" size="large" icon={<PlusOutlined />} onClick={onCreateGroup} className="min-h-[44px] w-full sm:w-auto">
            Create group
          </F1Button>
        </div>
      </div>

      {groups.length === 0 ? (
        <EmptyState
          title="No groups yet"
          description="Create a group to invite friends and start your league."
          buttonLabel="Create your first group"
          buttonIcon={<PlusOutlined />}
          onAction={onCreateGroup}
        />
      ) : (
        <GroupList groups={groups} />
      )}
    </div>
  );
}
