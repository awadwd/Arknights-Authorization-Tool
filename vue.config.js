// vue.config.js
// 开源版本：代理配置指向 GitHub
module.exports = {
    devServer: {
        port: 5173,
        disableHostCheck: true,
        proxy: {
            '/api': {
                target: 'https://raw.githubusercontent.com/awadwd/ArknightsAuthorization_Series-mirror/refs/heads/main',
                changeOrigin: true,
                secure: true,
                pathRewrite: {
                    '^/api': ''
                }
            },
            '/gitcode': {
                target: 'https://raw.gitcode.com',
                changeOrigin: true,
                secure: true,
                pathRewrite: {
                    '^/gitcode': ''
                }
            }
        }
    }
};