import { useEffect } from "react";
import { useAuth } from "@clerk/clerk-react";
import { App as AntdApp, ConfigProvider } from "antd";
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
    const jwtTemplate = import.meta.env.VITE_CLERK_JWT_TEMPLATE as string | undefined;
    useEffect(() => {
        setAuthTokenGetter(() =>
            jwtTemplate ? getToken({ template: jwtTemplate }) : getToken()
        );
        return () => setAuthTokenGetter(null);
    }, [getToken, jwtTemplate]);
    return null;
}

function App() {
    return (
        <>
            <ApiAuthSetup />
            <ConfigProvider theme={f1Theme}>
                <AntdApp>
                    <RecoilRoot>
                        <Index />
                    </RecoilRoot>
                </AntdApp>
            </ConfigProvider>
        </>
    );
}
export default App;
