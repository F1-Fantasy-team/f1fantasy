import { useEffect } from "react";
import { useAuth } from "@clerk/clerk-react";
import { ConfigProvider } from "antd";
import { RecoilRoot } from "recoil";
import { setAuthTokenGetter } from "./api/client";
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

/** Registers Clerk session token with the API client so requests send Authorization: Bearer <token>. */
function ApiAuthSetup() {
    const { getToken } = useAuth();
    useEffect(() => {
        setAuthTokenGetter(() => getToken());
        return () => setAuthTokenGetter(null);
    }, [getToken]);
    return null;
}

function App() {
    return (
        <>
            <ApiAuthSetup />
            <ConfigProvider theme={f1Theme}>
                <RecoilRoot>
                    <Index />
                </RecoilRoot>
            </ConfigProvider>
        </>
    );
}
export default App;
