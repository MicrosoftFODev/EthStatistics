import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
    plugins: [react()],

    base: "/EthStatistics/",

    server: {
        open: true,

        watch: {
            ignored: [
                "**/.vs/**",
                "**/FileContentIndex/**",
                "**/*.vsidx"
            ]
        }
    }
});