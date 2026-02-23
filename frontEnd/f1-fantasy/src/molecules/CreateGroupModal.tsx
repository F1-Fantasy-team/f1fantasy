import { useState } from "react";
import { App, Modal, Form, Input } from "antd";
import { F1Button, F1Title } from "../atoms";
import type { Group } from "../types/group";
import type { PredictionLockMode } from "../types/group";

type CreateGroupModalProps = {
  open: boolean;
  onClose: () => void;
  onCreated: (group: Group) => void;
  /** Create group via API. Returns the created group. */
  createGroup: (payload: { name: string; predictionLockMode: PredictionLockMode }) => Promise<Group>;
  /** Current user ID; set as group admin. */
  currentUserId: string;
};

const LOCK_MODE_OPTIONS: { value: PredictionLockMode; label: string; description: string }[] = [
  {
    value: "hybrid",
    label: "System + admin override",
    description: "System locks at season start; you can override lock/unlock anytime (default).",
  },
  {
    value: "admin",
    label: "Admin only",
    description: "You decide when predictions are locked or unlocked.",
  },
  {
    value: "system",
    label: "System only",
    description: "Lock is based on season start date from the backend; no manual override.",
  },
];

function LockModeCards({
  value,
  onChange,
}: {
  value?: PredictionLockMode;
  onChange?: (v: PredictionLockMode) => void;
}) {
  return (
    <div className="space-y-3">
      {LOCK_MODE_OPTIONS.map((opt) => {
        const selected = value === opt.value;
        return (
          <button
            key={opt.value}
            type="button"
            onClick={() => onChange?.(opt.value)}
            className={`flex w-full cursor-pointer items-start gap-4 rounded-xl border-2 p-4 text-left transition focus:outline-none focus:ring-2 focus:ring-f1-red/50 focus:ring-offset-2 focus:ring-offset-f1-carbon ${
              selected
                ? "border-f1-red bg-f1-red/10"
                : "border-f1-gray bg-f1-gray/40 hover:border-f1-silver/50 hover:bg-f1-gray/60"
            }`}
          >
            <span
              className={`mt-0.5 flex h-5 w-5 shrink-0 items-center justify-center rounded-full border-2 transition ${
                selected ? "border-f1-red bg-f1-red/20" : "border-f1-silver/50 bg-transparent"
              }`}
              aria-hidden
            >
              {selected && <span className="h-2 w-2 rounded-full bg-f1-red" />}
            </span>
            <div className="min-w-0 flex-1">
              <span className="block font-medium text-f1-silver">{opt.label}</span>
              <span className="mt-1 block text-xs leading-snug text-f1-silver/80">{opt.description}</span>
            </div>
          </button>
        );
      })}
    </div>
  );
}

export function CreateGroupModal({ open, onClose, onCreated, createGroup }: CreateGroupModalProps) {
  const { message } = App.useApp();
  const [form] = Form.useForm<{ name: string; predictionLockMode: PredictionLockMode }>();
  const [submitting, setSubmitting] = useState(false);

  const handleSubmit = () => {
    form.validateFields().then(async (values) => {
      setSubmitting(true);
      try {
        const group = await createGroup({
          name: values.name.trim(),
          predictionLockMode: values.predictionLockMode ?? "hybrid",
        });
        onCreated(group);
        form.resetFields();
        onClose();
      } catch (err) {
        message.error(err instanceof Error ? err.message : "Failed to create group");
      } finally {
        setSubmitting(false);
      }
    });
  };

  const handleCancel = () => {
    form.resetFields();
    onClose();
  };

  return (
    <Modal
      open={open}
      onCancel={handleCancel}
      footer={null}
      destroyOnHidden
      width={460}
      centered
      className="create-group-modal max-w-[calc(100vw-2rem)] [&_.ant-modal-content]:overflow-hidden [&_.ant-modal-content]:rounded-2xl [&_.ant-modal-content]:border [&_.ant-modal-content]:border-f1-gray [&_.ant-modal-content]:bg-f1-carbon [&_.ant-modal-content]:shadow-[0_24px_48px_-12px_rgba(0,0,0,0.6)] [&_.ant-modal-content]:border-t-[3px] [&_.ant-modal-content]:border-t-f1-red [&_.ant-modal-header]:border-f1-gray [&_.ant-modal-header]:bg-transparent [&_.ant-modal-body]:pt-6 [&_.ant-modal-close]:text-f1-silver [&_.ant-modal-close]:hover:text-f1-white"
      styles={{
        header: { borderBottomColor: "var(--color-f1-gray, #2d2d2d)", paddingBottom: 16 },
      }}
      title={
        <div className="pr-8">
          <F1Title level={4} className="mb-0 text-f1-white">
            Create a group
          </F1Title>
          <div className="mt-2 h-0.5 w-10 rounded-full bg-f1-red" aria-hidden />
        </div>
      }
    >
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        className="mt-2"
        requiredMark={false}
      >
        <Form.Item
          name="name"
          label={<span className="text-sm font-medium text-f1-silver">Group name</span>}
          rules={[
            { required: true, message: "Enter a group name" },
            { min: 2, message: "At least 2 characters" },
          ]}
        >
          <Input
            placeholder="e.g. Office Legends"
            className="h-11 rounded-lg border-f1-gray bg-f1-gray/80 text-f1-silver placeholder:text-f1-silver/50 focus:border-f1-red focus:shadow-[0_0_0_2px_rgba(225,6,0,0.2)] focus:outline-0"
            autoFocus
          />
        </Form.Item>

        <Form.Item
          name="predictionLockMode"
          label={<span className="text-sm font-medium text-f1-silver">When to lock predictions</span>}
          initialValue="hybrid"
        >
          <LockModeCards />
        </Form.Item>

        <div className="flex justify-end gap-3 pt-4">
          <F1Button onClick={handleCancel} className="min-w-[100px]">
            Cancel
          </F1Button>
          <F1Button type="primary" htmlType="submit" className="min-w-[120px]" disabled={submitting}>
            Create group
          </F1Button>
        </div>
      </Form>
    </Modal>
  );
}
