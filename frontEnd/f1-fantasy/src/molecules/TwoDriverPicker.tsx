import { useState } from "react";
import { Select } from "antd";
import { F1Button } from "../atoms";
import { useDrivers } from "../state/useDriversAndConstructors";

export type TwoDriverValue = { driverId1: string; driverId2: string };

const selectClass =
  "w-full [&_.ant-select-selector]:bg-f1-gray [&_.ant-select-selector]:border-f1-gray [&_.ant-select-selection-item]:text-f1-silver f1-driver-select";
const dropdownClass = "f1-driver-select-dropdown";

type TwoDriverPickerProps = {
  value: TwoDriverValue | undefined;
  onSave: (prediction: TwoDriverValue) => void;
  /** Optional labels, e.g. ["Driver 1", "Driver 2"] or ["First driver", "Second driver"] */
  labels?: [string, string];
};

export function TwoDriverPicker({ value, onSave, labels = ["Driver 1", "Driver 2"] }: TwoDriverPickerProps) {
  const drivers = useDrivers();
  const driverOptions = drivers.map((d) => ({ value: d.id, label: d.name }));
  const [driverId1, setDriverId1] = useState(value?.driverId1 ?? "");
  const [driverId2, setDriverId2] = useState(value?.driverId2 ?? "");

  const handleSave = () => {
    if (driverId1 && driverId2 && driverId1 !== driverId2) onSave({ driverId1, driverId2 });
  };

  const isValid = Boolean(driverId1 && driverId2 && driverId1 !== driverId2);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-4">
        <div className="min-w-48">
          <label className="mb-1 block text-xs text-f1-silver/70">{labels[0]}</label>
          <Select
            placeholder="Select driver"
            value={driverId1 || undefined}
            onChange={setDriverId1}
            options={driverOptions}
            className={selectClass}
            classNames={{ popup: { root: dropdownClass } }}
            allowClear
            showSearch
            filterOption={(input, option) =>
              (option?.label ?? "").toString().toLowerCase().includes(input.toLowerCase())
            }
            optionFilterProp="label"
          />
        </div>
        <div className="min-w-48">
          <label className="mb-1 block text-xs text-f1-silver/70">{labels[1]}</label>
          <Select
            placeholder="Select driver"
            value={driverId2 || undefined}
            onChange={setDriverId2}
            options={driverOptions.map((opt) => ({
              ...opt,
              disabled: opt.value === driverId1,
            }))}
            className={selectClass}
            classNames={{ popup: { root: dropdownClass } }}
            allowClear
            showSearch
            filterOption={(input, option) =>
              (option?.label ?? "").toString().toLowerCase().includes(input.toLowerCase())
            }
            optionFilterProp="label"
          />
        </div>
      </div>
      {isValid && (
        <F1Button type="primary" onClick={handleSave}>
          Save prediction
        </F1Button>
      )}
      {driverId1 && driverId2 && driverId1 === driverId2 && (
        <p className="text-xs text-amber-200">Pick two different drivers.</p>
      )}
    </div>
  );
}
