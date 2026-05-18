import { defineConfig } from 'vite'
import uni from '@dcloudio/vite-plugin-uni'

export default defineConfig({
  plugins: [uni()],
  server: {
    port: 5173, // 可选，保持与现在一致
    proxy: {
      '/api': {
        target: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, '')
      },
      '/gitcode': {
        target: 'https://raw.gitcode.com',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/gitcode/, '')
      }
    }
  }
})