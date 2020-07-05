import React, {useContext} from "react";
import {observer} from "mobx-react-lite";
import {Container, Dropdown, Menu, Image} from "semantic-ui-react";
import {Link, NavLink} from "react-router-dom";
import {RootStoreContext} from "../../app/stores/rootStore";

const NavBar: React.FC = () => {
    const rootStore = useContext(RootStoreContext);
    const {user, logout} = rootStore.userStore;

    return (
        <Menu fixed='top' inverted className={'nav'}>
            <Container>
                <Menu.Item header as={NavLink} exact to={'/'}>
                    <img
                        src='/assets/boat_launch_100.svg'
                        alt='logo'
                        style={{marginRight: '10px'}}
                    />

                </Menu.Item>
                <Menu.Item as={NavLink} exact to={'/slipways'}>
                    Slipanlagen
                </Menu.Item>
                <Menu.Item as={NavLink} exact to={'/waters'}>
                    Gewässer
                </Menu.Item>

                {user && (
                    <Menu.Item position='right'>
                        <Image
                            avatar
                            spaced='right'
                            src={'/assets/buoy_100.png'}
                        />

                        <Dropdown pointing='top left' text={user?.displayName}>
                            <Dropdown.Menu>
                                <Dropdown.Item
                                    as={Link}
                                    to={`/profile/username`}
                                    text='My profile'
                                    icon='user'
                                />
                                <Dropdown.Item
                                    onClick={logout}
                                    text='Logout'
                                    icon='power'
                                />
                                <Dropdown.Item
                                    as={Link}
                                    to={`/slipways/create`}
                                    text='Slipanlage'
                                    icon='add'
                                />
                            </Dropdown.Menu>
                        </Dropdown>
                    </Menu.Item>)}
            </Container>
        </Menu>
    )
};

export default observer(NavBar);
