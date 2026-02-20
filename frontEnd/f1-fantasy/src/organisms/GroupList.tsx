import { List } from "antd";
import { GroupCard } from "../molecules";
import type { Group } from "../types/group";

type GroupListProps = {
  groups: Group[];
};

export function GroupList({ groups }: GroupListProps) {
  return (
    <List
      grid={{ gutter: 12, xs: 1, sm: 1, md: 2 }}
      className="min-w-0"
      dataSource={groups}
      renderItem={(group) => (
        <List.Item>
          <GroupCard group={group} />
        </List.Item>
      )}
    />
  );
}
