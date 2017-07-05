export function stopPropogation<T>(e:React.SyntheticEvent<T>) {
    e.stopPropagation();
}