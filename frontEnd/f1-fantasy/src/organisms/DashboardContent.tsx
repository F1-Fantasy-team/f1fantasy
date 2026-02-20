import { PlusOutlined, TeamOutlined } from "@ant-design/icons";
import { F1Title, F1Button } from "../atoms";
import { EmptyState } from "../molecules";
import { GroupList } from "./GroupList";
import type { Group } from "../types/group";

type DashboardContentProps = {
  groups: Group[];
  onCreateGroup: () => void;
  onJoinGroup: () => void;
};

export function DashboardContent({ groups, onCreateGroup, onJoinGroup }: DashboardContentProps) {
  return (
    <div className="space-y-6 sm:space-y-8">
      <section className="rounded-lg border border-f1-gray bg-f1-carbon/50 space-y-3 p-3 sm:p-4">
        <F1Title level={5} className="!mb-1">How it works</F1Title>
        <p className="text-sm text-f1-silver">
          Create or join a group with an invite code. Make predictions across seven categories (drivers, constructors, draft, destructors, Mr Saturday, zero pointers, wildcard). Compare your picks with friends and see who scores the most when the season unfolds.
        </p>
        <F1Title level={5} className="!mb-1 !mt-2">Rules</F1Title>
        <p className="text-sm text-f1-silver">
          Points are awarded per category based on real-world results. Check each category for scoring details. The group standings show combined scores—highest total wins.
        </p>
      </section>

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
