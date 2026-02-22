import { GroupCard } from "../molecules";
import type { Group } from "../types/group";

type GroupListProps = {
  groups: Group[];
};

export function GroupList({ groups }: GroupListProps) {
  return (
    <div className="min-w-0 grid grid-cols-1 gap-3 md:grid-cols-2">
      {groups.map((group) => (
        <GroupCard key={group.id} group={group} />
      ))}
    </div>
  );
}
