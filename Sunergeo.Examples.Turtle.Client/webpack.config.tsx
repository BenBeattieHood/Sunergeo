var path = require('path');

var config = {
    entry: {
        "client" : path.resolve(__dirname, 'src/index'),				// use 'src/app' instead of 'src/app.tsx' because ts-loader will locate the file but cause a buffer overflow otherwise
    },
    output: {
        path: path.resolve(__dirname, 'build'),
        filename: '[name].bundle.js'
    },
    resolve: {
        extensions: ['.ts', '.tsx', '.js', '.jsx']
    },
    module: {
        loaders: [
            {
                test: /\.tsx?$/,
                loader: 'awesome-typescript-loader',
                exclude: /node_modules/
            },
            {
                test: /\.css$/,
                loader: ['style-loader', 'css-loader']
            },
            {
                test: /\.(woff2?|[ot]tf|eot|svg)$/,
                loader: 'url-loader?limit=10000'
            }
        ]
    },
    externals: {
        'react/lib/ExecutionEnvironment': true,
        'react/addons': true,
        'react/lib/ReactContext': 'window'
    },
    devServer: {
        contentBase: path.resolve(__dirname, 'build'),
        port: 8042,
        inline: false
    }
};

module.exports = config;
