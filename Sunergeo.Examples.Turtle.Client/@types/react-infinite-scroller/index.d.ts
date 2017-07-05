///<reference types="react"/>

declare namespace InfiniteScroller {
    interface InfiniteScrollProps {
        hasMore: boolean,
        initialLoad?: boolean,
        isReverse?: boolean,
        loadMore: (pageToLoad:number) => void,
        pageStart?: number,
        threshold?: number,
        useCapture?: boolean,
        useWindow?: boolean,
        loader?: React.ReactNode
    }
}

declare module "react-infinite-scroller" {
    import React = require("react");
    
    type InfiniteScroll = React.ReactElement<InfiniteScroller.InfiniteScrollProps>;
    var InfiniteScroll:React.ComponentClass<InfiniteScroller.InfiniteScrollProps>;

    export default InfiniteScroll;
}