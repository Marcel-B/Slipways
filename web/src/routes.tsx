import type { RouteObject } from 'react-router'
import { Layout } from './components/Layout'
import { AuthPage } from './pages/AuthPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { SlipwayDetailsPage } from './pages/SlipwayDetailsPage'
import { SlipwaysPage } from './pages/SlipwaysPage'

export const routes: RouteObject[] = [
  {
    element: <Layout />,
    children: [
      { index: true, element: <SlipwaysPage /> },
      { path: 'slipways/:id', element: <SlipwayDetailsPage /> },
      { path: 'login', element: <AuthPage mode="login" /> },
      { path: 'register', element: <AuthPage mode="register" /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]
