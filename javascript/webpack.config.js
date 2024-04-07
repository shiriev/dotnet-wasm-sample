const path = require('path');

module.exports = {
  entry: './src/index.js',
  output: {
    filename: 'main.js',
    path: path.resolve(__dirname, 'dist'),
  },
  devServer: {
    static: {
        directory: path.join(__dirname, 'dist'),
    },
    compress: true,
    port: 3000,
    liveReload: true,
    client: {
        webSocketURL: 'auto://0.0.0.0:0/proxy/3000/ws', // note the `:0` after `0.0.0.0`
    },
    allowedHosts: 'all',
  },
  mode: 'development',
};