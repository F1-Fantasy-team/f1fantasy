import { useState } from "react";
import { Select } from "antd";
import { F1Button } from "../atoms";
import { MOCK_DRIVERS } from "../data/mockDrivers";
import type { DriverDraftPrediction } from "../types/predictions";

const DRIVER_OPTIONS = MOCK_DRIVERS.map((d) => ({ value: d.id, label: d.name }));

type DriverDraftEditorProps = {
  value: DriverDraftPrediction | undefined;
  onSave: (prediction: DriverDraftPrediction) => void;
};

export function DriverDraftEditor({ value, onSave }: DriverDraftEditorProps) {
  const [driverId1, setDriverId1] = useState(value?.driverId1 ?? "");
  const [driverId2, setDriverId2] = useState(value?.driverId2 ?? "");

  const handleSave = () => {
    if (driverId1 && driverId2) onSave({ driverId1, driverId2 });
  };

  const isValid = Boolean(driverId1 && driverId2 && driverId1 !== driverId2);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-4">
        <div className="min-w-[12rem]">
          <label className="mb-1 block text-xs text-f1-silver/70">Driver 1</label>
          <Select
            placeholder="Select driver"
            value={driverId1 || undefined}
            onChange={setDriverId1}
            options={DRIVER_OPTIONS}
            className="w-full [&_.ant-select-selector]:bg-f1-gray [&_.ant-select-selector]:border-f1-gray [&_.ant-select-selection-item]:text-f1-silver"
            allowClear
          />
        </div>
        <div className="min-w-[12rem]">
          <label className="mb-1 block text-xs text-f1-silver/70">Driver 2</label>
          <Select
            placeholder="Select driver"
            value={driverId2 || undefined}
            onChange={setDriverId2}
            options={DRIVER_OPTIONS.map((opt) => ({
              ...opt,
              disabled: opt.value === driverId1,
            }))}
            className="w-full [&_.ant-select-selector]:bg-f1-gray [&_.ant-select-selector]:border-f1-gray [&_.ant-select-selection-item]:text-f1-silver"
            allowClear
          />
        </div>
      </div>
      <F1Button type="primary" onClick={handleSave} disabled={!isValid}>
        Save prediction
      </F1Button>
      {driverId1 && driverId2 && driverId1 === driverId2 && (
        <p className="text-xs text-amber-200">Pick two different drivers.</p>
      )}
    </div>
  );
}
