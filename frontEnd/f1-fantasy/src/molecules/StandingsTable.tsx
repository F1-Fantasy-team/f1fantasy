import { Table, Spin } from "antd";
import type { MemberStanding } from "../types/predictions";

type StandingsTableProps = {
  standings: MemberStanding[];
  currentUserId: string;
  isLoading?: boolean;
};

export function StandingsTable({ standings, currentUserId, isLoading }: StandingsTableProps) {
  const columns = [
    {
      title: "Rank",
      dataIndex: "rank",
      key: "rank",
      width: 72,
      render: (rank: number) => (
        <span className="font-mono text-f1-gold">{rank}</span>
      ),
    },
    {
      title: "Member",
      dataIndex: "displayName",
      key: "displayName",
      render: (name: string, record: MemberStanding) => {
        const raw = name && name.trim().length > 0 ? name.trim() : record.userId;
        const looksLikeUserId = raw.startsWith("user_");
        const baseName = looksLikeUserId ? (record.rank ? `Member ${record.rank}` : "Member") : raw;
        const effectiveName = record.userId === currentUserId ? (looksLikeUserId ? "You" : baseName) : baseName;
        return (
          <span className={record.userId === currentUserId ? "text-f1-red font-medium" : "text-f1-silver"}>
            {record.userId === currentUserId ? `${effectiveName} (you)` : effectiveName}
          </span>
        );
      },
    },
    {
      title: "Score",
      dataIndex: "overallScore",
      key: "overallScore",
      width: 100,
      render: (score: number) => (
        <span className="text-f1-white font-mono">{score}</span>
      ),
    },
  ];

  return (
    <div className="relative min-w-0 overflow-x-auto [-webkit-overflow-scrolling:touch]">
      {isLoading && (
        <div className="pointer-events-none absolute inset-0 z-10 flex items-center justify-center bg-f1-carbon/70 backdrop-blur-sm">
          <Spin />
        </div>
      )}
      <Table
        dataSource={standings}
        columns={columns}
        rowKey="userId"
        pagination={false}
        size="small"
        scroll={{ x: "max-content" }}
        className="[&_.ant-table]:bg-transparent [&_.ant-table-thead>tr>th]:bg-f1-gray [&_.ant-table-thead>tr>th]:text-f1-silver [&_.ant-table-thead>tr>th]:border-f1-gray [&_.ant-table-tbody>tr>td]:border-f1-gray [&_.ant-table-tbody>tr:hover>td]:bg-f1-gray/50"
      />
    </div>
  );
}
