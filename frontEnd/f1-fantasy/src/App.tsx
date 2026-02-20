import { ConfigProvider } from "antd";
import { RecoilRoot } from "recoil";
import Index from "./pages/Index.tsx";

const f1Theme = {
    token: {
        colorPrimary: "#e10600",
        colorBgContainer: "#1a1a1a",
        colorBgElevated: "#2d2d2d",
        colorBorder: "#2d2d2d",
        colorText: "#e5e5e5",
        colorTextSecondary: "rgba(229, 229, 229, 0.7)",
    },
};

function App() {
    return (
        <ConfigProvider theme={f1Theme}>
            <RecoilRoot>
                <Index />
            </RecoilRoot>
        </ConfigProvider>
    );
}
export default App;
